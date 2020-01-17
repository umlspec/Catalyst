#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///   Manages the IPFS Configuration.
    /// </summary>
    public class ConfigController : IpfsController
    {
        /// <summary>
        ///   Details on a configuration setting.
        /// </summary>
        public class ConfigDetailDto
        {
            /// <summary>
            ///   The name of the configuration setting.
            /// </summary>
            public string Key;

            /// <summary>
            ///   The value of the configuration setting.
            /// </summary>
            public JToken Value;
        }

        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public ConfigController(IDfsService dfs) : base(dfs) { }

        /// <summary>
        ///  Get all the configuration settings.
        /// </summary>
        [HttpGet, HttpPost, Route("config/show")]
        public Task<JObject> GetAllKeys() { return IpfsCore.ConfigApi.GetAsync(Cancel); }

        /// <summary>
        ///   Gets or sets the configuration setting.
        /// </summary>
        /// <param name="arg">
        ///   The configuration setting key and possibly its value.
        /// </param>
        /// <param name="json">
        ///   Indicates that the value is JSON.
        /// </param>
        /// <returns></returns>
        [HttpGet, HttpPost, Route("config")]
        public async Task<ConfigDetailDto> Key(string[] arg,
            bool json = false)
        {
            if (arg.Length == 1)
            {
                var value = await IpfsCore.ConfigApi.GetAsync(arg[0], Cancel);
                return new ConfigDetailDto
                {
                    Key = arg[0],
                    Value = value
                };
            }
            else if (arg.Length == 2)
            {
                if (json)
                {
                    var value = JToken.Parse(arg[1]);
                    await IpfsCore.ConfigApi.SetAsync(arg[0], value, Cancel);
                    return new ConfigDetailDto
                    {
                        Key = arg[0],
                        Value = value
                    };
                }

                // Else a text value
                await IpfsCore.ConfigApi.SetAsync(arg[0], arg[1], Cancel);
                return new ConfigDetailDto
                {
                    Key = arg[0],
                    Value = arg[1]
                };
            }
            else
            {
                throw new FormatException("Too many arg values.");
            }
        }

        /// <summary>
        ///  Replace all the configuration settings.
        /// </summary>
        /// <param name="file">
        ///   The new configuration settings.
        /// </param>
        [HttpGet, HttpPost, Route("config/replace")]
        public async Task Replace(IFormFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            await using (var stream = file.OpenReadStream())
            using (var text = new StreamReader(stream))
            using (var reader = new JsonTextReader(text))
            {
                var json = await JObject.LoadAsync(reader);
                await IpfsCore.ConfigApi.ReplaceAsync(json);
            }
        }
    }
}