﻿using Dapper;
using DevChatter.DevStreams.Core.Model;
using DevChatter.DevStreams.Core.Services;
using DevChatter.DevStreams.Core.Settings;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DevChatter.DevStreams.Infra.Dapper.Services
{
    public class TagService : ITagService
    {
        private readonly DatabaseSettings _dbSettings;

        public TagService(IOptions<DatabaseSettings> databaseSettings)
        {
            _dbSettings = databaseSettings.Value;
        }

        public async Task<ILookup<int, Tag>> GetChannelTagsLookup(IEnumerable<int> channelIds)
        {
            var channelTags = new Dictionary<int, IEnumerable<Tag>>();

            const string channelSql = "SELECT Id FROM Channels WHERE Id IN @ChannelIds";
            const string extraSql =
                @"SELECT t.* FROM ChannelTags ct INNER JOIN Tags t ON t.Id = ct.TagId WHERE ct.ChannelId = @id";

            using (IDbConnection connection = new SqlConnection(_dbSettings.DefaultConnection))
            {
                var channels = (await connection.QueryAsync<Channel>(channelSql, new { ChannelIds = channelIds })).ToList();

                foreach (var channel in channels)
                {
                    using (var multi = await connection.QueryMultipleAsync(extraSql, new { channel.Id }))
                    {
                        channel.Tags = (await multi.ReadAsync<Tag>()).ToList();
                    }

                    channelTags.Add(channel.Id, channel.Tags);
                }

                return channelTags.SelectMany(p => p.Value
                       .Select(x => new { p.Key, Value = x }))
                       .ToLookup(pair => pair.Key, pair => pair.Value);
            }
        }

        public async Task<List<Tag>> GetTagsInUse()
        {
            const string sql = "SELECT * FROM Tags WHERE Id in (SELECT DISTINCT TagId FROM ChannelTags)";

            using (IDbConnection connection = new SqlConnection(_dbSettings.DefaultConnection))
            {
                var tagsUsedByChannels = (await connection.QueryAsync<Tag>(sql)).ToList();

                return tagsUsedByChannels;
            }
        }
    }
}
