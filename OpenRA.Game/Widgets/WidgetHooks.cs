#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Reflection;

namespace OpenRA.Widgets
{
	public static class WidgetHooks
	{
		public static Func<T> HookGet<T>( object self, MemberInfo member )
		{
			switch( member.MemberType )
			{
			case MemberTypes.Property:
				return (Func<T>)Delegate.CreateDelegate( typeof( Func<T> ), self, ( (PropertyInfo)member ).GetGetMethod() );
			default:
				throw new NotImplementedException();
			}
		}

		public static Action<T> HookSet<T>( object self, MemberInfo member )
		{
			switch( member.MemberType )
			{
			case MemberTypes.Property:
				return (Action<T>)Delegate.CreateDelegate( typeof( Action<T> ), self, ( (PropertyInfo)member ).GetSetMethod() );
			default:
				throw new NotImplementedException();
			}
		}

		public static Action<T> HookAction<T>( object self, MemberInfo member )
		{
			switch( member.MemberType )
			{
			case MemberTypes.Method:
				return (Action<T>)Delegate.CreateDelegate( typeof( Action<T> ), self, ( (MethodInfo)member ) );
			default:
				throw new NotImplementedException();
			}
		}
	}

	public class HookingWidgetDelegate : IWidgetDelegate
	{
		public HookingWidgetDelegate()
		{
			foreach( var member in this.GetType().GetMembers( BindingFlags.Public | BindingFlags.Instance ) )
				foreach( var hook in (WidgetHookAttribute[])member.GetCustomAttributes( typeof( WidgetHookAttribute ), true ) )
					Widget.RootWidget.GetWidget( hook.WidgetName ).ApplyHook( hook.Event, this, member );
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method )]
	public class WidgetHookAttribute : Attribute
	{
		public readonly string Event;
		public readonly string WidgetName;

		public WidgetHookAttribute( string widgetEvent, string widgetName )
		{
			Event = widgetEvent;
			WidgetName = widgetName;
		}
	}
}
