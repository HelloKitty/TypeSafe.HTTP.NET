﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Castle.Core.Internal;

namespace TypeSafe.Http.Net
{
	public sealed class HeaderServiceCallInterpreter : IHeaderServiceCallInterpreter
	{
		/// <inheritdoc />
		public IEnumerable<IRequestHeader> ProduceFromContext(IServiceCallContext serviceContext, IServiceCallParametersContext parameters)
		{
			if (serviceContext == null) throw new ArgumentNullException(nameof(serviceContext));
			if (parameters == null) throw new ArgumentNullException(nameof(parameters));

			//TODO: Caching
			return AddServiceWideHeaders(serviceContext.ServiceType)
				.Concat(AddMethodSpecificHeaders(serviceContext.ServiceMethod))
				.Concat(AddDynamicHeaders(serviceContext.ServiceMethod.GetParameters(), parameters.Parameters))
				.ToArray();
		}

		//TODO: We need to cache this stuff, it will get expensive.
		private IEnumerable<IRequestHeader> AddDynamicHeaders(IEnumerable<ParameterInfo> parameters, object[] parameterValues)
		{
			List<IRequestHeader> headers = new List<IRequestHeader>(parameterValues.Length / 2 + 1); //a pretty good guess.

			int i = -1;
			foreach (ParameterInfo pi in parameters)
			{
				i++;

				DynamicHeaderAttribute dynamicHeaderAttribute = pi.GetCustomAttribute<DynamicHeaderAttribute>();

				if (dynamicHeaderAttribute == null)
					continue;

				headers.Add(new BasicRequestHeader(dynamicHeaderAttribute.HeaderType, parameterValues[i]?.ToString()));
			}

			return headers;
		}

		private IEnumerable<IRequestHeader> AddServiceWideHeaders(Type type)
		{
			//TODO: How do we handle multiple of the same header in seperate metadata?
			return type.GetAttributes<HeaderAttribute>()
				.Select(h => new BasicRequestHeader(h.HeaderType, h.ValueString)).ToArray();
		}

		private IEnumerable<IRequestHeader> AddMethodSpecificHeaders(MethodInfo mi)
		{
			return mi.GetCustomAttributes<HeaderAttribute>()
				.Select(h => new BasicRequestHeader(h.HeaderType, h.ValueString)).ToArray();
		}
	}
}
