﻿/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using ScriptEngine.HostedScript.Library;
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace oscript
{
    [ContextClass("ВебЗапрос","WebRequest")]
    public class WebRequestContext : AutoContext<WebRequestContext>
    {
        MapImpl _environmentVars = new MapImpl();
        MapImpl _get = new MapImpl();
        MapImpl _post = new MapImpl();
        string _post_raw_utf = null;
        byte[] _post_raw = null;

        public WebRequestContext()
        {
            string get = Environment.GetEnvironmentVariable("QUERY_STRING");
            if(get != null)
            {
                FillGetMap(get);
            }

            string contentLen = Environment.GetEnvironmentVariable("CONTENT_LENGTH");
            if(contentLen != null)
            {
                FillPostMap(contentLen);
            }

            FillEnvironmentVars();

        }

        private void FillEnvironmentVars()
        {
            foreach (DictionaryEntry item in Environment.GetEnvironmentVariables())
            {
                _environmentVars.Insert(
                    ValueFactory.Create((string)item.Key),
                    ValueFactory.Create((string)item.Value));
            }
        }

        private void FillPostMap(string contentLen)
        {
            int len = Int32.Parse(contentLen);
            if (len > 0)
            {
                _post_raw = new byte[len];
                using (var stdin = Console.OpenStandardInput())
                {
                    stdin.Read(_post_raw, 0, len);
                }

                // TODO: Подумать, где задавать кодировку разбора сырого запроса
                //       или вообще убрать разбор по-умолчанию, оставив только двоичные данные
                _post_raw_utf = Encoding.UTF8.GetString(_post_raw);

                ParseFormData(_post_raw_utf, _post);
            }
        }

        private void FillGetMap(string get)
        {
            ParseFormData(get, _get);
        }

        private void ParseFormData(string data, MapImpl map)
        {
            var pairs = data.Split('&');
            foreach (var pair in pairs)
            {
                var nameVal = pair.Split(new Char[] { '=' }, 2);
                if (nameVal.Length == 2)
                {
                    IValue key = ValueFactory.Create(nameVal[0]);
                    IValue val = ValueFactory.Create(Decode(nameVal[1]));
                    map.Insert(key, val);
                }
                else if(pair.Length > 0)
                {
                    IValue val = ValueFactory.Create(Decode(pair));
                    map.Insert(val, ValueFactory.Create());
                }
            }
        }

        private static string Decode(string p)
        {
            byte[] bytes = new byte[p.Length];

            int j = 0;
            for (int i = 0; i < p.Length; i++, j++)
            {
                if (p[i] == '+')
                    bytes[j] = 0x20;
                else if (p[i] == '%')
                {
                    bytes[j] = ByteFromHEX(p, ref i);
                }
                else
                    bytes[j] = (byte)p[i];
            }

            return Encoding.UTF8.GetString(bytes, 0, j);

        }

        private static byte ByteFromHEX(string pattern, ref int index)
        {
            System.Diagnostics.Debug.Assert(pattern[index] == '%');

            index++;
            int major = 0;
            int minor = 0;
            for (int i = 0; i < 2; i++)
            {
                int code = (int)pattern[index];
                if (code >= 48 && code <= 57)
                    code = code - 48;
                else if (code >= 65 && code <= 70)
                    code = code - 55;

                if (i == 0)
                {
                    major = code;
                    index++;
                }
                else
                    minor = code;
            }

            return (byte)(major * 16 + minor);

        }

        [ContextProperty("GET")]
        public IValue GET
        {
            get
            {
                return _get;
            }
        }

        [ContextProperty("POST")]
        public IValue POST
        {
            get
            {
                return _post;
            }
        }

        [ContextProperty("ENV")]
        public IValue ENV
        {
            get
            {
                return _environmentVars;
            }
        }

        [ContextProperty("RAWDATA")]
        public IValue RAWDATA
        {
            get
            {
                if (_post_raw == null)
                    return ValueFactory.Create ();
                return new BinaryDataContext (_post_raw);
            }
        }

        [ContextProperty("POSTDATA")]
        public IValue POSTDATA
        {
            get
            {
                if (_post_raw_utf == null)
                    return ValueFactory.Create ();
                return ValueFactory.Create (_post_raw_utf);
            }
        }
    }
}
