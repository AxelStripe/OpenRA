#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class ColorBlockWidget : Widget
	{
		public Func<Color> GetColor;

		public ColorBlockWidget()
			: base()
		{
			GetColor = () => Color.White;
		}

		protected ColorBlockWidget(ColorBlockWidget widget)
			: base(widget)
		{
			GetColor = widget.GetColor;
		}

		public override Widget Clone()
		{
			return new ColorBlockWidget(this);
		}

		public override void DrawInner()
		{
			WidgetUtils.FillRectWithColor(RenderBounds, GetColor());
		}
	}
}
