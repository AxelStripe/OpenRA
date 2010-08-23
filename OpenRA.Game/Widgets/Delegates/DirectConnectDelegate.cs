#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;

namespace OpenRA.Widgets.Delegates
{
	public class DirectConnectDelegate : IWidgetDelegate
	{
		public DirectConnectDelegate()
		{
			var r = Widget.RootWidget;
			var dc = r.GetWidget("DIRECTCONNECT_BG");

			dc.GetWidget("JOIN_BUTTON").OnMouseUp = mi =>
			{

				var address = dc.GetWidget<TextFieldWidget>("SERVER_ADDRESS").Text;
				var cpts = address.Split(':').ToArray();
				if (cpts.Length != 2)
					return true;

				Game.Settings.LastServer = address;
				Game.Settings.Save();

				Widget.CloseWindow();
				Game.JoinServer(cpts[0], int.Parse(cpts[1]));
				return true;
			};

			dc.GetWidget("CANCEL_BUTTON").OnMouseUp = mi =>
			{
				Widget.CloseWindow();
				return r.GetWidget("MAINMENU_BUTTON_JOIN").OnMouseUp(mi);
			};
		}
	}
}
