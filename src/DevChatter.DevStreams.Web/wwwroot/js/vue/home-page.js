﻿let app = new Vue({
    el: "#mainContent",
    data: {
        liveChannelsIsLoaded: false,
        liveChannels : [],
        hasStream: false,
        errorMessage: '',
        appstyles: { "display": "none" }
    },
    mounted() {
        this.fetchLiveChannels();
        this.appstyles = { "display": "block"}
    },
    methods: {
        fetchLiveChannels: function () {
            axios.get(`/api/IsLive/`)
                .then(response => {
                    this.liveChannels = response.data;
                    this.liveChannelsIsLoaded = true;
                })
                .catch(error => {
                    console.log(error.statusText);
                });
        },
        fetchStream: function () {
            axios.get(`?handler=Lucky`)
                .then(response => {
                        if (response.data.error) {
                            this.errorMessage = response.data.error;
                        } else if (this.hasStream) {
                            var src = `https://player.twitch.tv/?allowfullscreen&playsinline&player=twitch_everywhere&targetOrigin=https%3A%2F%2Fembed.twitch.tv&channel=${response.data.channelName}&origin=https%3A%2F%2Fembed.twitch.tv`;
                            $('iframe').attr('src', src);
                        } else if (!this.hasStream) {
                            new Twitch.Embed("twitch-embed", {
                                width: 854,
                                height: 480,
                                channel: response.data.channelName
                            });
                            this.hasStream = true;
                        }
                    },
                    error => {
                        console.log(error.statusText);
                    });
        }
    }
});