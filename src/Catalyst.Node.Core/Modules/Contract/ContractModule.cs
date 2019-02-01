﻿using Autofac;
using Catalyst.Node.Common.Modules;

namespace Catalyst.Node.Core.Modules.Contract
{
    public class ContractModule : Module
    {

        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="contractSettings"></param>
        public static ContainerBuilder Load(ContainerBuilder builder)
        {
            builder.Register(c => Node.Core.Modules.Contract.Contract.GetInstance())
                .As<IContract>()
                .SingleInstance();
            return builder;
        }
    }
}