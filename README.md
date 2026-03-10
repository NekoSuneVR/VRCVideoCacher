# VRCVideoCacher

### What is VRCVideoCacher?

This repository is a fork of the original [EllyVR/VRCVideoCacher](https://github.com/EllyVR/VRCVideoCacher), adapted and managed around the NekoSuneVR video infrastructure.

This fork is focused on simple video proxying for VRChat and Resonite. It does not expect you to download and manage video files on your own PC. NekoSuneVR servers handle the heavy lifting for you, including remote downloading, proxy delivery, and server-side storage workflows.

If you want, this fork can also optionally cache the final proxied video to your own PC for faster repeat playback, but that behavior is off by default.

### How does it work?

It replaces VRChat's `yt-dlp.exe` with our own stub, and the local client rewrites supported video URLs into the NekoSuneVR video proxy endpoint. From there, the server handles the actual media retrieval and playback delivery.

In practice, the app is intentionally simple:
- paste or play a supported URL
- the client proxies the URL
- the video plays

No local video cache management is required for normal use.

Auto install missing codecs: [VP9](https://apps.microsoft.com/detail/9n4d0msmp0pt) | [AV1](https://apps.microsoft.com/detail/9mvzqvxjbq9v) | [AC-3](https://apps.microsoft.com/detail/9nvjqjbdkn97)

### What changed in this fork?

- Forked from the original EllyVR project and reworked for NekoSuneVR-managed servers.
- No need to manually download and store video files on your PC for normal playback.
- No need to provide your own YouTube cookies. NekoSuneVR manages the server-side handling for you.
- Safer for end users because your personal YouTube account cookies are not being used locally by the client.
- Playback is proxy-based: the app mainly rewrites URLs and lets the server do the rest.
- Added Suno integration so Suno links can be played through the video player.

### Supported links

- YouTube
- Suno
- Direct media links that can be proxied through the video bypass endpoint

### Suno integration

This fork adds Suno link support through the proxy site, allowing Suno songs to be used directly in supported video players.

### Why no YouTube cookies?

Unlike the upstream local-first workflow, this fork is built around NekoSuneVR-managed server infrastructure. That means the cookie management, downloading flow, proxying, and media delivery are handled server-side instead of on your local machine.

That is one of the main safety improvements in this fork. You are not expected to inject your own YouTube cookies into the client, so your personal account is less exposed to risk from local use. NekoSuneVR manages the server-side account flow and delivery pipeline for you.

You can think of it like a managed yt-dlp API flow for VRChat-style video playback: the client asks the server for the playable URL path, the server handles the source platform side, and the player gets the proxied result. This makes it easier to support more sources quickly without pushing the hard parts onto the user's PC.

This also means platform support can move faster. As the server-side integrations improve, new sites and media sources can be added much more quickly.

This fork is also useful for users in places where YouTube access is blocked, restricted, or unreliable. In some regions, people may need to use a VPN just to reach YouTube at all, which is not practical or fair when they only want to play a song or video inside a game. A proxy-based approach helps reduce that friction and makes more of that content usable again, including music that may only be available on YouTube.

### Fix YouTube videos sometimes failing to play

> Loading failed. File not found, codec not supported, video resolution too high or insufficient system resources.

Sync system time, Open Windows Settings -> Time & Language -> Date & Time, under "Additional settings" click "Sync now"

Edit `Config.json` and set `ytdlDelay` to something like `10` seconds.

### Fix cached videos failing to play in public instances

> Attempted to play an untrusted URL (Domain: localhost) that is not allowlisted for public instances.

Run notepad as Admin then browse to `C:\Windows\System32\drivers\etc\hosts` add this new line `127.0.0.1 localhost.youtube.com` to the bottom of the file, edit `Config.json` and set `ytdlWebServerURL` to `http://localhost.youtube.com:9696`

### Running on Linux

- Install `dotnet-runtime-10.0`
- Run with `./VRCVideoCacher.Client`
- For this fork, the client mainly handles local URL proxying and patching. The media-side work is expected to be handled by NekoSuneVR infrastructure.

### Project layout

This fork now keeps only the client application and the `yt-dlp-stub` replacement used for player patching.

Build with:
- Client: `dotnet build VRCVideoCacher.Client.sln -c Release`

### TODO

- Add livestream support for Kick
- Add livestream support for DLive
- Add more platforms over time through the same proxy system
- Expand site-side integrations for more supported sources
- Keep improving the managed server-side media pipeline so new platforms can be supported with minimal client changes

### Uninstalling

- If you have VRCX, delete the startup shortcut "VRCVideoCacher" from `%AppData%\VRCX\startup`
- Delete "yt-dlp.exe" from `%AppData%\..\LocalLow\VRChat\VRChat\Tools` and restart VRChat or rejoin world.

### Config Options

| Option                    | Description                                                                                                                                                                                                                                                                                    |
| ------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ytdlWebServerURL          | Used to circumvent VRChat public world video player whitelist, see above for usage instructions.                                                                                                                                                                                               |
| WebServerBindUrls         | List of local bind URLs for the web server, e.g. `[ "http://127.0.0.1:9696" ]` or `[ "http://0.0.0.0:9696" ]`. Leave empty to use localhost defaults.                                                                                                                                        |
| BlockedUrls               | List of URLs to never load in VRC, also works for blocking domains e.g. `[ "https://youtube.com", "https://youtu.be" ]` to block YouTube.                                                                                                                                                      |
| BlockRedirect             | Video to load in-place of Blocked URL.                                                                                                                                                                                                                                                         |
| PatchResonite             | Enable Resonite support.                                                                                                                                                                                                                                                                       |
| PatchVRC                  | Enable VRChat support.                                                                                                                                                                                                                                                                         |
| AutoUpdate                | When a update is available for VRCVideoCacher it will automatically be installed.                                                                                                                                                                                                              |
| LocalVideoCacheEnabled    | When `true`, the client downloads the final proxied media once and serves it from your own PC on future plays. Default is `false`, so the managed server-side flow stays primary.                                                                                                              |
| LocalVideoCachePath       | Optional custom folder for locally cached videos. Leave empty to use the default cache folder inside the app data directory.                                                                                                                                                                   |
| VideoBypassBaseUrls       | Ordered list of video bypass base URLs used by this fork, e.g. `[ "https://dl.nekosunevr.co.uk", "https://dl.ballisticok.xyz" ]`. The client probes these in order and uses the first healthy endpoint.                                                                                     |
