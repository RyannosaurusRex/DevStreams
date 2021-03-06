﻿using DevChatter.DevStreams.Core.Data;
using DevChatter.DevStreams.Core.Model;
using DevChatter.DevStreams.Core.Twitch;
using DevChatter.DevStreams.Web.Authorization;
using DevChatter.DevStreams.Web.Data.ViewModel.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DevChatter.DevStreams.Web.Controllers
{
    [Route("api/[controller]")]
    public class ChannelsController : Controller
    {
        private readonly IChannelAggregateService _channelService;
        private readonly ITwitchChannelService _twitchService;
        private readonly ICrudRepository _crudRepository;

        public ChannelsController(IChannelAggregateService channelService,
            ICrudRepository crudRepository, ITwitchChannelService twitchService)
        {
            _channelService = channelService;
            _crudRepository = crudRepository;
            _twitchService = twitchService;
        }

        [Authorize, ChannelAuthorize("id")]
        [HttpGet, Route("{id}")]
        public ChannelEditModel Get(int id)
        {
            if (id <= 0)
            {
                return new ChannelEditModel();
            }

            var channel = _channelService.GetAggregate(id);

            var editModel = channel?.ToChannelEditModel();

            return editModel;
        }

        [Authorize, ChannelAuthorize("channel")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChannelEditModel channel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                var twitchChannel = await _twitchService.GetChannelInfo(channel.Name);

                if (IsNewChannel(channel))
                {
                    Channel model = new Channel();
                    model.ApplyEditChanges(channel);
                    model.ApplyTwitchChanges(twitchChannel);

                    var userId = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

                    await _channelService.Create(model, userId);
                }
                else
                {
                    Channel model = _channelService.GetAggregate(channel.Id);
                    model.ApplyEditChanges(channel);
                    model.ApplyTwitchChanges(twitchChannel);
                    await _channelService.Update(model);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ChannelExists(channel.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return Ok();

            bool IsNewChannel(ChannelEditModel c) => c.Id <= 0;
        }

        private async Task<bool> ChannelExists(int id)
        {
            return await _crudRepository.Exists<Channel>(id);
        }
    }
}