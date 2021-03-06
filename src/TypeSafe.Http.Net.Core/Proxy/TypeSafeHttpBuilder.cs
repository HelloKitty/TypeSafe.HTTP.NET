﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Contributors;

namespace TypeSafe.Http.Net
{
	/// <summary>
	/// Builder for the type safe HTTP service.
	/// </summary>
	/// <typeparam name="THttpServiceInterface">The service type.</typeparam>
	public sealed class TypeSafeHttpBuilder<THttpServiceInterface> : IHttpServiceProxyBuilder<THttpServiceInterface>, ISerializationStrategyRegister, IHttpClientServiceRegister
		where THttpServiceInterface : class
	{
		/// <summary>
		/// The client to use.
		/// </summary>
		private IHttpServiceProxy Client { get; set; }

		private ContentSerializationFactory SerializerFactory { get; }

		static TypeSafeHttpBuilder()
		{
			//Enforce that it is in an interface.
			if(!typeof(THttpServiceInterface).GetTypeInfo().IsInterface)
				throw new NotImplementedException($"Cannot create service proxy for non interfaces. Type: {typeof(THttpServiceInterface).FullName} is not an interface type.");
		}

		/// <summary>
		/// Creates a new service builder that can be configured for
		/// REST/HTTP/Web use.
		/// </summary>
		/// <returns>A new builder for the <typeparamref name="THttpServiceInterface"/> service interface.</returns>
		public static TypeSafeHttpBuilder<THttpServiceInterface> Create()
		{
			return new TypeSafeHttpBuilder<THttpServiceInterface>();
		}

		internal TypeSafeHttpBuilder()
		{
			SerializerFactory = new ContentSerializationFactory();
		}

		/// <inheritdoc />
		public THttpServiceInterface Build()
		{
			//I can't think of a good reason we shouldn't allow multiple to be built.
			//so we won't prevent multiple calls to build.
			return new ProxyGenerator()
				.CreateInterfaceProxyWithoutTarget<THttpServiceInterface>(new HttpServiceCallAsyncCallInterceptor(new RequestContextFactory(new HeaderServiceCallInterpreter()), Client, SerializerFactory, SerializerFactory).ToInterceptor());
		}

		/// <inheritdoc />
		public bool Register<TBodySerializerMetadataType, TSerializerType>(TSerializerType serializationService) 
			where TBodySerializerMetadataType : BodyContentAttribute 
			where TSerializerType : IResponseDeserializationStrategy, IRequestSerializationStrategy, IContentTypeAssociable
		{
			if (serializationService == null) throw new ArgumentNullException(nameof(serializationService));
			if (SerializerFactory == null) throw new InvalidOperationException($"Serialization factory is null and never should be.");

			//Just forward it to the factory
			return SerializerFactory.Register<TBodySerializerMetadataType, TSerializerType>(serializationService);
		}

		/// <inheritdoc />
		public void Register(IHttpServiceProxy proxy)
		{
			//TODO: Should we throw if a client is already set?
			Client = proxy;
		}
	}
}
