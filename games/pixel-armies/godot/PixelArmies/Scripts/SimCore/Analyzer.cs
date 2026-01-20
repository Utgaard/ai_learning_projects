using System;
using System.Collections.Generic;

namespace PixelArmies.SimCore;

public sealed class MatchupStats
{
    public int Runs;
    public int LeftWins;
    public int RightWins;

    public float AvgTimeToWin;
    public float AvgWinnerBaseHpRemaining;

    public int Stomps; // winner base hp > 70%

    public override string ToString()
    {
        float lw = Runs > 0 ? 100f * LeftWins / Runs : 0f;
        float rw = Runs > 0 ? 100f * RightWins / Runs : 0f;
        float stompRate = Runs > 0 ? 100f * Stomps / Runs : 0f;

        return
$@"Runs: {Runs}
Left wins: {LeftWins} ({lw:0.0}%)
Right wins: {RightWins} ({rw:0.0}%)
Avg time-to-win: {AvgTimeToWin:0.0}s
Avg winner base HP remaining: {AvgWinnerBaseHpRemaining:0.0}
Stomp rate (>70% base HP): {stompRate:0.0}%";
    }
}

public static class Analyzer
{
    public static MatchupStats RunMany(SimConfig cfg, ArmyDef left, ArmyDef right, int runs, int seedBase)
    {
        var stats = new MatchupStats { Runs = runs };

        float sumTime = 0f;
        float sumWinHp = 0f;

        for (int i = 0; i < runs; i++)
        {
            int seed = seedBase + i;
            var sim = new BattleSimulator(cfg, left, right, seed);

            // Hard cap to avoid infinite battles in early tuning
            const float maxTime = 240f; // 4 minutes
            while (!sim.State.IsOver && sim.State.Time < maxTime)
                sim.Step(SimConfig.FixedDt);

            // If timed out, treat as "no result" => count as right win for now (arbitrary).
            // We can make draws explicit later.
            var winner = sim.State.IsOver ? sim.State.Winner : Side.Right;
            float winnerHp = winner == Side.Left ? sim.State.LeftBaseHp : sim.State.RightBaseHp;

            if (winner == Side.Left) stats.LeftWins++;
            else stats.RightWins++;

            sumTime += sim.State.Time;
            sumWinHp += Math.Max(0f, winnerHp);

            if (winnerHp > 0.70f * cfg.BaseMaxHp) stats.Stomps++;
        }

        stats.AvgTimeToWin = sumTime / Math.Max(1, runs);
        stats.AvgWinnerBaseHpRemaining = sumWinHp / Math.Max(1, runs);
        return stats;
    }
}
