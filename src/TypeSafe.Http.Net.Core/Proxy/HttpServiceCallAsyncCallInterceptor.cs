﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace TypeSafe.Http.Net
{
	public sealed class HttpServiceCallAsyncCallInterceptor : IAsyncInterceptor
	{
		/// <summary>
		/// The factory for building contexts.
		/// </summary>
		private IRequestContextFactory RequestContextFactory { get; }

		private IHttpServiceProxy ProxyClient { get; }

		private ISerializationStrategyFactory SerializerFactory { get; }

		private IDeserializationStrategyFactory DeserializerFactory { get; }

		public HttpServiceCallAsyncCallInterceptor(IRequestContextFactory requestContextFactory, IHttpServiceProxy proxyClient, ISerializationStrategyFactory serializerFactory, IDeserializationStrategyFactory deserializerFactory)
		{
			if (requestContextFactory == null) throw new ArgumentNullException(nameof(requestContextFactory));
			if (proxyClient == null) throw new ArgumentNullException(nameof(proxyClient));
			if (serializerFactory == null) throw new ArgumentNullException(nameof(serializerFactory));
			if (deserializerFactory == null) throw new ArgumentNullException(nameof(deserializerFactory));

			RequestContextFactory = requestContextFactory;
			ProxyClient = proxyClient;
			SerializerFactory = serializerFactory;
			DeserializerFactory = deserializerFactory;
		}

		/// <inheritdoc />
		public void InterceptSynchronous(IInvocation invocation)
		{
			throw new InvalidOperationException($"TypeSafe.Http.Net only supports async. Type: {invocation.TargetType.FullName} Method: {invocation.Method.Name} must have a return type of Task or Task<T>.");
		}

		/// <inheritdoc />
		public void InterceptAsynchronous(IInvocation invocation)
		{
			//Per documentation of https://github.com/JSkimming/Castle.Core.AsyncInterceptor this will be called for Task based return type methods.
			//It will ONLY be called if it is of type Task and not Task<T> where no return value is expected.
			invocation.ReturnValue = AsyncNoReturn(invocation);
		}

		public async Task AsyncNoReturn(IInvocation invocation)
		{
			IHttpClientRequestContext context = RequestContextFactory.CreateContext(new CastleCoreInvocationCallContextAdapter(invocation),
				new CastleCoreInvocationParametersContextAdapter(invocation));

			//If it has no body we don't need to provide or produce serializers for it.
			if (!context.BodyContext.HasBody)
			{
				await ProxyClient.Send(context).ConfigureAwait(false);
				return;
			}

			//We need to look at the request to determine which serializer strategy should be used.
			IRequestSerializationStrategy serializer = SerializerFactory.SerializerFor(context.BodyContext.ContentAttributeType);

			//Because we don't need to get any returned information we can just send it
			await ProxyClient.Send(context, serializer).ConfigureAwait(false);
		}

		public async Task<TResult> AsyncWithReturn<TResult>(IInvocation invocation)
		{
			//TODO: Handle return data deserialization.
			IHttpClientRequestContext context = RequestContextFactory.CreateContext(new CastleCoreInvocationCallContextAdapter(invocation),
				new CastleCoreInvocationParametersContextAdapter(invocation));

			//If it has no body we don't need to provide or produce serializers for it.
			if (!context.BodyContext.HasBody)
			{
				return await ProxyClient.Send<TResult>(context, DeserializerFactory);
			}

			//We need to look at the request to determine which serializer strategy should be used.
			IRequestSerializationStrategy serializer = SerializerFactory.SerializerFor(context.BodyContext.ContentAttributeType);

			//Because we don't need to get any returned information we can just send it
			return await ProxyClient.Send<TResult>(context, serializer, DeserializerFactory).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public void InterceptAsynchronous<TResult>(IInvocation invocation)
		{
			//Per documentation of https://github.com/JSkimming/Castle.Core.AsyncInterceptor this will be called for Task based return type methods
			//that are of type Task<T>
			invocation.ReturnValue = AsyncWithReturn<TResult>(invocation);
		}
	}
}
