﻿/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ScriptEngine.Machine
{
    class ComReflectionNameToIdMapper
    {
        private readonly Type _reflectedType;

        readonly IndexedNamesCollection _propertyNames;

        readonly IndexedNamesCollection _methodNames;

        readonly List<PropertyInfo> _propertyCache;

        readonly List<System.Reflection.MethodInfo> _methodsCache;

        public ComReflectionNameToIdMapper(Type type)
        {
            _reflectedType = type;
            _propertyNames = new IndexedNamesCollection();
            _methodNames = new IndexedNamesCollection();
            _propertyCache = new List<PropertyInfo>();
            _methodsCache = new List<System.Reflection.MethodInfo>();
        }

        public int FindProperty(string name)
        {
            int id;
            var hasProperty = _propertyNames.TryGetIdOfName(name, out id);
            if(!hasProperty)
            {
                var propInfo = _reflectedType.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                if (propInfo == null)
                    throw RuntimeException.PropNotFoundException(name);

                id = _propertyNames.RegisterName(name);
                System.Diagnostics.Debug.Assert(_propertyCache.Count == id);
                
                _propertyCache.Add(propInfo);
                
            }

            return id;
        }

        public int FindMethod(string name)
        {
            int id;
            var hasMethod = _methodNames.TryGetIdOfName(name, out id);
            if (!hasMethod)
            {
                var methodInfo = _reflectedType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if(methodInfo == null)
                    throw RuntimeException.MethodNotFoundException(name);

                id = _methodNames.RegisterName(name);
                System.Diagnostics.Debug.Assert(_methodsCache.Count == id);

                _methodsCache.Add(methodInfo);
            }

            return id;
        }

        public PropertyInfo GetProperty(int id)
        {
            return _propertyCache[id];
        }

        public System.Reflection.MethodInfo GetMethod(int id)
        {
            return _methodsCache[id];
        }
    }
}
