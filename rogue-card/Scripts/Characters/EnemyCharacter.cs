using Godot;
using System.Collections.Generic;

/// <summary>
/// Represents an enemy-controlled character on the battle board.
/// Mirrors PlayerCharacter structure; AI decision logic will be added in M4.
/// </summary>
public partial class EnemyCharacter : Node3D
{
    [Export] public CharacterData Data { get; set; }

    public int CurrentHp    { get; private set; }
    public int MaxHp        { get; private set; }
    public int CurrentMana  { get; private set; }
    public int MaxMana      { get; private set; }

    public Vector2I GridPosition { get; set; } = Vector2I.Zero;

    public CardDeck Deck { get; } = new();
    public CardHand Hand { get; } = new();

    private HealthBar3D _healthBar;

    public bool IsAlive => CurrentHp > 0;

    public override void _Ready()
    {
        _healthBar = new HealthBar3D();
        AddChild(_healthBar);

        if (Data != null)
            InitialiseFromData(Data);
    }

    public void InitialiseFromData(CharacterData data)
    {
        Data        = data;
        MaxHp       = data.BaseHp;
        CurrentHp   = MaxHp;
        MaxMana     = data.BaseMana;
        CurrentMana = MaxMana;
        Hand.MaxHandSize = data.HandSize;
        Deck.InitialiseDeck(data.StartingDeck);
        _healthBar?.UpdateHealth(CurrentHp, MaxHp, isEnemy: true);
        GD.Print($"[EnemyCharacter] Initialised: {data.ClassName} | HP:{MaxHp}");
    }

    public void ModifyHp(int amount)
    {
        CurrentHp = Mathf.Clamp(CurrentHp - amount, 0, MaxHp);
        _healthBar?.UpdateHealth(CurrentHp, MaxHp, isEnemy: true);
        if (CurrentHp <= 0)
            GD.Print($"[EnemyCharacter] {Data?.ClassName} defeated.");
    }

    /// <summary>Draw cards up to hand limit.</summary>
    public void DrawToHandLimit()
    {
        while (Hand.FreeSlotsCount > 0)
        {
            var card = Deck.Draw();
            if (card == null) break;
            Hand.AddCard(card);
        }
    }

    // -------------------------------------------------------------------------
    // Utility AI Decisions (M4)
    // -------------------------------------------------------------------------

    /// <summary>AI decides where to move during the Move Phase.</summary>
    public Vector2I DecideMoveTarget(BattleBoard board, List<PlayerCharacter> players, System.Collections.Generic.Dictionary<ulong, Vector2I> lockedPlayerMoves)
    {
        if (players.Count == 0) return GridPosition;

        Vector2I bestMove = GridPosition;
        float bestScore = -999f;
        
        // Find closest player for simple heuristic, using their CONFIRMED future position if available
        PlayerCharacter targetPlayer = null;
        Vector2I targetPlayerFuturePos = Vector2I.Zero;
        float minDistToPlayer = float.MaxValue;
        
        foreach (var p in players)
        {
            if (!p.IsAlive) continue;
            
            // Assume they will be where they locked in to move. If for some reason they aren't in the dictionary, use current pos.
            Vector2I futurePos = lockedPlayerMoves.ContainsKey(p.SteamId) ? lockedPlayerMoves[p.SteamId] : p.GridPosition;
            
            float dist = MetricManhattan(GridPosition, futurePos);
            if (dist < minDistToPlayer)
            {
                minDistToPlayer = dist;
                targetPlayer = p;
                targetPlayerFuturePos = futurePos;
            }
        }
        
        if (targetPlayer == null) return GridPosition; // All dead

        // Find optimal range based on hand cards
        int optimalRange = 1; // Default to melee pursuit if no cards
        int highestDamage = -1;

        foreach (var card in Hand.Cards)
        {
            if (card.CardType == CardType.Battle && card.BaseDamage > highestDamage)
            {
                highestDamage = card.BaseDamage;
                optimalRange = card.Range;
            }
        }

        int moveRange = Data.MoveRange;
        var validMoves = new List<Vector2I>();

        // 1. Gather all valid tiles in MoveRange
        for (int q = -moveRange; q <= moveRange; q++)
        {
            for (int r = -moveRange; r <= moveRange; r++)
            {
                if (Mathf.Abs(q) + Mathf.Abs(r) <= moveRange)
                {
                    Vector2I checkPos = GridPosition + new Vector2I(q, r);

                    if (board.IsInBounds(checkPos))
                    {
                        var occupant = board.GetOccupant(checkPos);
                        
                        // It is blocked if:
                        // 1. Someone is currently standing there (and we don't assume they are moving out of the way to be safe)
                        // 2. OR someone has locked in a move to go there in the future
                        bool blocked = (occupant != null && occupant != this) || lockedPlayerMoves.ContainsValue(checkPos);
                        
                        if (!blocked)
                        {
                            validMoves.Add(checkPos);
                        }
                    }
                }
            }
        }

        // 2. Score them
        var bestMoves = new List<Vector2I>();
        foreach (var move in validMoves)
        {
            float distToTarget = MetricManhattan(move, targetPlayerFuturePos);
            
            // The score is negatively impacted by how far away it is from the optimal range.
            // i.e., distance to sweet spot. If distToTarget == optimalRange, penalty is 0.
            float score = -Mathf.Abs(distToTarget - optimalRange);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            else if (Mathf.IsEqualApprox(score, bestScore)) // Float safe equality check
            {
                bestMoves.Add(move);
            }
        }

        // 3. Pick randomly from the best options to avoid predictable sliding
        if (bestMoves.Count > 0)
        {
            bestMove = bestMoves[GD.RandRange(0, bestMoves.Count - 1)];
        }

        return bestMove;
    }

    /// <summary>AI decides which card to play during the Battle Phase.</summary>
    public QueuedAction? DecideCardPlay(BattleBoard board, List<PlayerCharacter> players, List<EnemyCharacter> enemies)
    {
        if (Hand.Cards.Count == 0 || players.Count == 0) return null;

        QueuedAction? bestAction = null;
        float bestScore = -0.1f; // Minimum score threshold to play a card
        var bestActions = new List<QueuedAction>();

        foreach (var card in Hand.Cards)
        {
            // Calculate valid tiles for this card
            var validTargets = GetValidTargetCells(board, card);
            
            foreach (var target in validTargets)
            {
                // Score the action on this specific target
                float score = EvaluateCardUtility(board, card, target, players, enemies);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestActions.Clear();
                    bestActions.Add(new QueuedAction(Data, card, target, GridPosition));
                }
                else if (score >= 0 && Mathf.IsEqualApprox(score, bestScore))
                {
                    bestActions.Add(new QueuedAction(Data, card, target, GridPosition));
                }
            }
        }

        if (bestActions.Count > 0)
        {
            bestAction = bestActions[GD.RandRange(0, bestActions.Count - 1)];
        }

        return bestAction;
    }

    // -------------------------------------------------------------------------
    // AI Helpers
    // -------------------------------------------------------------------------

    private float EvaluateCardUtility(BattleBoard board, CardData card, Vector2I targetCell, List<PlayerCharacter> players, List<EnemyCharacter> enemies)
    {
        float score = 0;
        var affectedCells = board.GetCellsInAoE(targetCell, card.AoeShape, GridPosition);

        foreach (var cell in affectedCells)
        {
            var occupant = board.GetOccupant(cell);
            if (occupant == null) continue;

            if (occupant is PlayerCharacter p && p.IsAlive)
            {
                // Dealing damage to a player is good
                if (card.BaseDamage > 0)
                {
                    // Weight killing blows higher
                    if (p.CurrentHp <= card.BaseDamage) score += card.BaseDamage * 2f; 
                    else score += card.BaseDamage;
                }
                
                // Healing a player is very bad (unless we introduce undeaed mechanics later)
                if (card.BaseHealing > 0) score -= card.BaseHealing * 2f;
            }
            else if (occupant is EnemyCharacter e && e.IsAlive)
            {
                // Dealing damage to a fellow enemy is BAD (Friendly Fire)
                if (card.BaseDamage > 0) score -= card.BaseDamage * 1.5f;

                // Healing a fellow enemy is GOOD
                if (card.BaseHealing > 0)
                {
                    // Only good if they are actually missing HP
                    int missingHp = e.MaxHp - e.CurrentHp;
                    int actualHealing = Mathf.Min(missingHp, card.BaseHealing);
                    score += actualHealing;
                }
            }
        }

        return score;
    }

    private List<Vector2I> GetValidTargetCells(BattleBoard board, CardData card)
    {
        var validCells = new List<Vector2I>();
        int range = card.Range;

        for (int q = -range; q <= range; q++)
        {
            for (int r = -range; r <= range; r++)
            {
                if (Mathf.Abs(q) + Mathf.Abs(r) <= range)
                {
                    var cell = GridPosition + new Vector2I(q, r);
                    if (board.IsInBounds(cell))
                    {
                        bool isValid = false;
                        switch (card.Target)
                        {
                            case TargetType.Self:
                                isValid = (cell == GridPosition);
                                break;
                            case TargetType.SingleEnemy: // Actually means "Single Target" usually depending on your design
                                isValid = board.IsOccupied(cell); // AI can consider targeting any occupied cell
                                break;
                            case TargetType.AnyTile:
                                isValid = true;
                                break;
                            case TargetType.Global:
                                isValid = true;
                                break;
                        }

                        if (isValid) validCells.Add(cell);
                    }
                }
            }
        }
        return validCells;
    }

    private float MetricManhattan(Vector2I a, Vector2I b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
    }
}
