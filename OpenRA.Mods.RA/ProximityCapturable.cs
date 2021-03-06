﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
    public class ProximityCapturableInfo : ITraitInfo
    {
        public readonly bool Permanent = false;
        public readonly int Range = 5;
        public readonly bool MustBeClear = false;
        public readonly string[] CaptorTypes = { "Vehicle", "Tank", "Infantry" };

        public object Create(ActorInitializer init) { return new ProximityCapturable(init.self, this); }
    }

    public class ProximityCapturable : ITick, ISync
    {
        [Sync]
        public readonly Player OriginalOwner;

        public ProximityCapturableInfo Info;

        [Sync]
        public bool Captured;

        public Actor Self;

        public ProximityCapturable(Actor self, ProximityCapturableInfo info)
        {
            Info = info;
            Self = self;
            OriginalOwner = self.Owner;
        }

        public void Tick(Actor self)
        {
            if (Captured && Info.Permanent) return; // Permanent capture

            if (!Captured)
            {
                var captor = GetInRange(self);

                if (captor != null)
                {
                    if (Info.MustBeClear && !IsClear(self, captor.Owner, OriginalOwner)) return;

                    ChangeOwnership(self, captor, OriginalOwner);
                }

                return;
            }

            // if the area must be clear, and there is more than 1 player nearby => return ownership to default
            if (Info.MustBeClear && !IsClear(self, self.Owner, OriginalOwner))
            {
                // Revert Ownership
                ChangeOwnership(self, self.Owner, OriginalOwner);
                return;
            }

            // See if the 'temporary' owner still is in range
            if (!IsStillInRange(self))
            {
                // no.. So find a new one
                var captor = GetInRange(self);

                if (captor != null) // got one
                {
                    ChangeOwnership(self, captor, self.Owner);
                    return;
                }

                // Revert Ownership otherwise
                ChangeOwnership(self, self.Owner, OriginalOwner);
            }
        }

        private void ChangeOwnership(Actor self, Player previousOwner, Player originalOwner)
        {
            self.World.AddFrameEndTask(w =>
            {
                if (self.Destroyed) return;

                // momentarily remove from world so the ownership queries don't get confused
                w.Remove(self);
                self.Owner = originalOwner;
                w.Add(self);

                if (self.Owner == self.World.LocalPlayer)
                    w.Add(new FlashTarget(self));

                Captured = false;

                foreach (var t in self.TraitsImplementing<INotifyCapture>())
                    t.OnCapture(self, self, previousOwner, self.Owner);
            });
        }

        private void ChangeOwnership(Actor self, Actor captor, Player previousOwner)
        {
            self.World.AddFrameEndTask(w =>
            {
                if (self.Destroyed || (captor.Destroyed || !captor.IsInWorld)) return;

                // momentarily remove from world so the ownership queries don't get confused
                w.Remove(self);
                self.Owner = captor.Owner;
                w.Add(self);

                if (self.Owner == self.World.LocalPlayer)
                    w.Add(new FlashTarget(self));

                Captured = true;

                foreach (var t in self.TraitsImplementing<INotifyCapture>())
                    t.OnCapture(self, captor, previousOwner, self.Owner);
            });
        }

        static bool AreMutualAllies(Player a, Player b)
        {
            return a.Stances[b] == Stance.Ally &&
                b.Stances[a] == Stance.Ally;
        }

        bool CanBeCapturedBy(Actor a)
        {
            return a.HasTrait<ProximityCaptor>() && a.Trait<ProximityCaptor>().HasAny(Info.CaptorTypes);
        }

        IEnumerable<Actor> UnitsInRange()
        {
            return Self.World.FindUnitsInCircle(Self.CenterLocation, Game.CellSize * Info.Range)
                .Where(a => a.IsInWorld && a != Self && !a.Destroyed)
                .Where(a => !a.Owner.NonCombatant);
        }

        bool IsClear(Actor self, Player currentOwner, Player originalOwner)
        {
            return UnitsInRange().Where(a => a.Owner != originalOwner)
                .Where(a => a.Owner != currentOwner)
                .Where(a => CanBeCapturedBy(a))
                .All(a => AreMutualAllies(a.Owner, currentOwner));
        }

        // TODO exclude other NeutralActor that arent permanent
        bool IsStillInRange(Actor self)
        {
            return UnitsInRange()
                .Where(a => a.Owner == self.Owner)
                .Where(a => CanBeCapturedBy(a))
                .Any();
        }

        IEnumerable<Actor> CaptorsInRange(Actor self)
        {
            return UnitsInRange()
                .Where(a => a.Owner != OriginalOwner)
                .Where(a => CanBeCapturedBy(a));
        }

        // TODO exclude other NeutralActor that arent permanent
        Actor GetInRange(Actor self)
        {
            return CaptorsInRange(self).OrderBy(a => (a.CenterLocation - self.CenterLocation).LengthSquared)
                .FirstOrDefault();
        }

        int CountPlayersNear(Actor self, Player ignoreMe)
        {
            return CaptorsInRange(self).Select(a => a.Owner)
                .Distinct().Count(p => p != ignoreMe);
        }
    }
}
