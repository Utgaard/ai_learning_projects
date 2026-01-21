#nullable enable

using System;
using System.Collections.Generic;

namespace PixelArmies.SimCore;

public sealed class MatchupStats
{
    public int Runs;
    public int LeftWins;
    public int RightWins;
    public int Draws;

    public float AvgTimeToWin;
    public float AvgWinnerBaseHpRemaining;

    public int Stomps; // winner base hp > 70%
    public float MaxSeconds;
    public string TimeoutPolicy = "lower enemy base HP wins; ties draw";

    public override string ToString()
    {
        float lw = Runs > 0 ? 100f * LeftWins / Runs : 0f;
        float rw = Runs > 0 ? 100f * RightWins / Runs : 0f;
        float dw = Runs > 0 ? 100f * Draws / Runs : 0f;
        float stompRate = Runs > 0 ? 100f * Stomps / Runs : 0f;

        return
$@"Runs: {Runs}
Left wins: {LeftWins} ({lw:0.0}%)
Right wins: {RightWins} ({rw:0.0}%)
Draws: {Draws} ({dw:0.0}%)
Avg time-to-win: {AvgTimeToWin:0.0}s
Avg winner base HP remaining: {AvgWinnerBaseHpRemaining:0.0}
Stomp rate (>70% base HP): {stompRate:0.0}%
Max sim time: {MaxSeconds:0.0}s (timeout policy: {TimeoutPolicy})";
    }
}

public static class Analyzer
{
    public static MatchupStats RunMany(
        SimConfig cfg,
        ArmyDef left,
        ArmyDef right,
        int runs,
        int seedBase,
        float maxSeconds,
        int progressInterval = 0,
        Action<int, int>? progressCallback = null)
    {
        var stats = new MatchupStats { Runs = runs };

        float sumTime = 0f;
        float sumWinHp = 0f;
        stats.MaxSeconds = maxSeconds;

        for (int i = 0; i < runs; i++)
        {
            int seed = seedBase + i;
            var sim = new BattleSimulator(cfg, left, right, seed);

            // Hard cap to avoid infinite battles in early tuning
            float maxTime = maxSeconds > 0f ? maxSeconds : 240f;
            while (!sim.State.IsOver && sim.State.Time < maxTime)
            {
                sim.Step(SimConfig.FixedDt);
                sim.ConsumeDamageEvents();
                sim.ConsumeUnitDiedEvents();
            }

            Side? winner = null;
            if (sim.State.IsOver)
            {
                winner = sim.State.Winner;
            }
            else
            {
                if (sim.State.RightBaseHp < sim.State.LeftBaseHp) winner = Side.Left;
                else if (sim.State.LeftBaseHp < sim.State.RightBaseHp) winner = Side.Right;
            }

            float winnerHp = 0f;
            if (winner == Side.Left)
            {
                stats.LeftWins++;
                winnerHp = sim.State.LeftBaseHp;
            }
            else if (winner == Side.Right)
            {
                stats.RightWins++;
                winnerHp = sim.State.RightBaseHp;
            }
            else
            {
                stats.Draws++;
            }

            sumTime += sim.State.Time;
            sumWinHp += Math.Max(0f, winnerHp);

            if (winnerHp > 0.70f * cfg.BaseMaxHp) stats.Stomps++;

            if (progressCallback != null && progressInterval > 0)
            {
                int current = i + 1;
                if (current % progressInterval == 0 || current == runs)
                {
                    progressCallback(current, runs);
                }
            }
        }

        stats.AvgTimeToWin = sumTime / Math.Max(1, runs);
        stats.AvgWinnerBaseHpRemaining = sumWinHp / Math.Max(1, runs);
        return stats;
    }
}
