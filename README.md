# VRCVideoCacher

### What is VRCVideoCacher?

This repository is a fork of the original [EllyVR/VRCVideoCacher](https://github.com/EllyVR/VRCVideoCacher), adapted and managed around the NekoSuneVR video infrastructure.

This fork is focused on simple video proxying for VRChat and Resonite. It does not expect you to download and manage video files on your own PC. NekoSuneVR servers handle the heavy lifting for you, including remote downloading, proxy delivery, and server-side storage workflows.

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

### Split Projects (Server/Client)

This repo includes two separate projects:
- `VRCVideoCacher.Server` for server-side downloader/proxy work.
- `VRCVideoCacher.Client` for the proxy/patch client used by players.

Build with:
- Server: `dotnet build VRCVideoCacher.Server.sln -c Release`
- Client: `dotnet build VRCVideoCacher.Client.sln -c Release`

### TODO

- Add livestream support for Kick
- Add livestream support for DLive
- Add more platforms over time through the same proxy system
- Expand site-side integrations for more supported sources

### Uninstalling

- If you have VRCX, delete the startup shortcut "VRCVideoCacher" from `%AppData%\VRCX\startup`
- Delete "yt-dlp.exe" from `%AppData%\..\LocalLow\VRChat\VRChat\Tools` and restart VRChat or rejoin world.

### Config Options

Some options below still come from the upstream codebase. In this fork, the most important part is the local proxy client and the remote NekoSuneVR-managed server flow.

| Option                    | Description                                                                                                                                                                                                                                                                                    |
| ------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ytdlWebServerURL          | Used to circumvent VRChats public world video player whitelist, see above for usage instructions.                                                                                                                                                                                              |
| ytdlPath                  | Path to the yt-dlp executable, default `Utils\\yt-dlp.exe`, when set to `""` it will use global PATH instead, as a side effect this will disable the yt-dlp, ffmpeg and deno auto updater.                                                                                                     |
| ytdlUseCookies            | Legacy upstream option. In the NekoSuneVR fork you normally do not need local YouTube cookies because the server side manages that workflow.                                                                                                                                                   |
| ytdlAutoUpdate            | Auto update yt-dlp, ffmpeg and deno.                                                                                                                                                                                                                                                           |
| ytdlAdditionalArgs        | Add your own [yt-dlp args](https://github.com/yt-dlp/yt-dlp?tab=readme-ov-file#usage-and-options) (only add these if you know what you're doing)                                                                                                                                               |
| ytdlDubLanguage           | Set preferred audio language for AVPro and cached videos, be warned you may end up with auto translated slop. e.g. `de` for German, check list of [supported lang codes](https://github.com/yt-dlp/yt-dlp/blob/c26f9b991a0681fd3ea548d535919cec1fbbd430/yt_dlp/extractor/youtube.py#L381-L390) |
| ytdlDelay                 | No delay (Default) `0`, YouTube videos can fail to load sometimes in-game without this delay. e.g. `8` for 8 seconds.                                                                                                                                                                          |
| YouTubePoTokenUrl         | Base URL for the bgutil PoToken HTTP server, e.g. `http://127.0.0.1:4416`. When set, VRCVideoCacher passes `--extractor-args "youtubepot-bgutilhttp:base_url=..."` to yt-dlp (unless already present in `ytdlAdditionalArgs`).                                                              |
| CachedAssetPath           | Location to store downloaded videos, e.g. store videos on separate drive with `D:\\DownloadedVideos`                                                                                                                                                                                           |
| WebServerBindUrls         | List of local bind URLs for the web server, e.g. `[ "http://127.0.0.1:9696" ]` or `[ "http://0.0.0.0:9696" ]`. Leave empty to use localhost defaults.                                                                                                                                        |
| BlockedUrls               | List of URLs to never load in VRC, also works for blocking domains e.g. `[ "https://youtube.com", "https://youtu.be" ]` to block YouTube.                                                                                                                                                      |
| BlockRedirect             | Video to load in-place of Blocked URL.                                                                                                                                                                                                                                                         |
| CacheYouTube              | Download YouTube videos to `CachedAssets` to improve load times next time the video plays.                                                                                                                                                                                                     |
| CacheYouTubeMaxResolution | Maximum resolution to cache youtube videos in (Larger resolutions will take longer to cache), e.g. `2160` for 4K.                                                                                                                                                                              |
| CacheYouTubeMaxLength     | Maximum video duration in minutes, e.g. `60` for 1 hour.                                                                                                                                                                                                                                       |
| CacheMaxSizeInGb          | Maximum size of `CachedAssets` folder in GB, `0` for Unlimited.                                                                                                                                                                                                                                |
| CacheEvictUnusedMinutes   | Delete cached files that have not been accessed for this many minutes, `0` to disable.                                                                                                                                                                                                        |
| CacheEvictIntervalMinutes | How often to run the unused cache cleanup, `0` to disable.                                                                                                                                                                                                                                     |
| CachePyPyDance            | Download videos that play while you're in [PyPyDance](https://vrchat.com/home/world/wrld_f20326da-f1ac-45fc-a062-609723b097b1)                                                                                                                                                                 |
| CacheVRDancing            | Download videos that play while you're in [VRDancing](https://vrchat.com/home/world/wrld_42377cf1-c54f-45ed-8996-5875b0573a83)                                                                                                                                                                 |
| PatchResonite             | Enable Resonite support.                                                                                                                                                                                                                                                                       |
| PatchVRC                  | Enable VRChat support.                                                                                                                                                                                                                                                                         |
| AutoUpdate                | When a update is available for VRCVideoCacher it will automatically be installed.                                                                                                                                                                                                              |
| PreCacheUrls              | Download all videos from a JSON list format e.g. `[{"fileName":"video.mp4","url":"https:\/\/example.com\/video.mp4","lastModified":1631653260,"size":124029113},...]` "lastModified" and "size" are optional fields used for file integrity.                                                   |
| RemoteServerEnabled       | Enable proxying `/api/getvideo` and `/api/youtube-cookies` to remote servers.                                                                                                                                                                                                                  |
| RemoteServerYouTubeOnly   | Only proxy YouTube requests to remote servers when enabled.                                                                                                                                                                                                                                    |
| RemoteServerFallbackToLocal | If all remote servers fail, fall back to local handling.                                                                                                                                                                                                                                     |
| RemoteServerDisableLocalCache | Disable local cache usage and downloads when remote proxying is enabled.                                                                                                                                                                                                                    |
| RemoteServerTimeoutSeconds | Timeout for remote server requests.                                                                                                                                                                                                                                                           |
| RemoteServerUrls          | List of remote server base URLs for proxying, in order of priority, e.g. `[ "https://server1.example.com", "https://server2.example.com" ]`.                                                                                                                                                   |
| VideoBypassBaseUrls       | Ordered list of video bypass base URLs used by this fork, e.g. `[ "https://dl.nekosunevr.co.uk", "https://dl.ballisticok.xyz" ]`. The client probes these in order and uses the first healthy endpoint.                                                                                     |

> Generate PoToken has unfortunately been [deprecated](https://github.com/iv-org/youtube-trusted-session-generator?tab=readme-ov-file#tool-is-deprecated)
