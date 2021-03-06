#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class WithRotorInfo : ITraitInfo
	{
		public readonly string Id = "rotor";
		public readonly int[] Offset = { 0, 0 };
		public object Create(ActorInitializer init) { return new WithRotor(init.self, this); }
	}

	public class WithRotor : ITick
	{
		public Animation rotorAnim;
		public WithRotor(Actor self, WithRotorInfo info)
		{
			var rs = self.Trait<RenderSimple>();
			var facing = self.Trait<IFacing>();

			rotorAnim = new Animation(rs.GetImage(self));
			rotorAnim.PlayRepeating("rotor");
			rs.anims.Add(info.Id, new RenderSimple.AnimationWithOffset(
				rotorAnim,
				() => Combat.GetTurretPosition( self, facing, new Turret(info.Offset)),
				null ) { ZOffset = 1 } );
		}

		public void Tick(Actor self)
		{
			var isFlying = self.Trait<IMove>().Altitude > 0 && !self.IsDead();
			if (isFlying ^ (rotorAnim.CurrentSequence.Name != "rotor")) 
				return;

			rotorAnim.ReplaceAnim(isFlying ? "rotor" : "slow-rotor");
		}
	}
}
