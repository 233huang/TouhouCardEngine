﻿using System;
using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface IEffect
    {
        [Obsolete]
        string[] events { get; }
        string[] getEvents(ITriggerManager manager);
        string[] piles { get; }
        void register(IGame game, ICard card);
        void unregister(IGame game, ICard card);
        bool checkCondition(IGame game, IPlayer player, ICard card, object[] vars);
        bool checkTarget(IGame game, IPlayer player, ICard card, object[] targets);
        Task execute(IGame game, IPlayer player, ICard card, object[] vars, object[] targets);
    }
}
