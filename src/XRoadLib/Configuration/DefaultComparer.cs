﻿using System;
using System.Collections.Generic;
using System.Reflection;
using XRoadLib.Extensions;

namespace XRoadLib.Configuration
{
    internal class DefaultComparer : IComparer<PropertyInfo>
    {
        internal static readonly IComparer<PropertyInfo> Instance = new DefaultComparer(null);

        private readonly ITypeConfiguration typeConfiguration;

        internal DefaultComparer(ITypeConfiguration typeConfiguration)
        {
            this.typeConfiguration = typeConfiguration;
        }

        public int Compare(PropertyInfo x, PropertyInfo y)
        {
            return string.Compare(x.GetPropertyName(typeConfiguration), y.GetPropertyName(typeConfiguration), StringComparison.InvariantCultureIgnoreCase);
        }
    }
}