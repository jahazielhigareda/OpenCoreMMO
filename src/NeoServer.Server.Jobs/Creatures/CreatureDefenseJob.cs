﻿using NeoServer.Game.Contracts.Creatures;
using NeoServer.Game.Common.Creatures;
using NeoServer.Server.Tasks;

namespace NeoServer.Server.Jobs.Creatures
{
    public class CreatureDefenseJob
    {
        private const uint INTERVAL = 1000;
        public static void Execute(ICombatActor creature, Game game)
        {
            if (!(creature is IMonster monster))
            {
                return;
            }
            if (monster.IsDead)
            {
                return;
            }
            if (monster.IsInCombat && !monster.Defending)
            {
                var interval = monster.Defend();

                ScheduleDefense(game, monster, interval);
            }
        }

        private static void ScheduleDefense(Game game, IMonster monster, ushort interval)
        {

            if (monster.Defending)
            {
                game.Scheduler.AddEvent(new SchedulerEvent(interval, () =>
                {
                    var interval = monster.Defend();
                    ScheduleDefense(game, monster, interval);
                }));
            }
        }
    }
}
