﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TypeSafe.Http.Net
{
	/// <summary>
	/// Metadata marker that indicates an object should be serialized in the body
	/// using a JSON serializer.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class JsonBodyAttribute : BodyContentAttribute
	{
		public JsonBodyAttribute()
			: base()
		{
			
		}
	}
}
