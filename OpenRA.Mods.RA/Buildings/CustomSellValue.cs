﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	// allow a nonstandard sell/repair value to avoid
	// buy-sell exploits like c&c's PROC.

	public class CustomSellValueInfo : TraitInfo<CustomSellValue>
	{
		public readonly int Value = 0;
	}

	public class CustomSellValue { }
}
