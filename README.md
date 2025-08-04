<h1 align="center">Jellyfin TubeArchivist Plugin</h1>

<p align="center">
<img alt="Plugin Banner" src="https://raw.githubusercontent.com/tubearchivist/tubearchivist-jf-plugin/master/images/logo.png"/>
<br/>
</p>

> [!IMPORTANT]
> Jellyfin release cycle has changed in the past few months and now it is shorter than before. The plugin supports only the latest Jellyfin release, in order to continue using this plugin with all the latest features you will need to upgrade your Jellyfin installation.
> The same rule applies to TubeArchivist: the plugin is only guaranteed to work with the latest TubeArchivist version.

> [!WARNING]
> Jellyfin 10.10 introduced a bug that prevents the plugin to correctly create Season folders by year. The bug has been finally solved on the Jellyfin codebase, but, until the next minor release, a manual build of the Jellyfin branch `release-10.10.z` is required in order to get the fix running.<br>
> This is not a plugin bug, any issue opened about this bug will be immediately closed!

## About

<p>This plugin adds the metadata provider for <a href="https://www.tubearchivist.com/">TubeArchivist</a>, offering improved flexibility and native integration with Jellyfin compared to previous solutions.</p>

## How it works
The media organization is a `Shows` collection, where each channel is a show and its videos are the episodes, organized in seasons by year.<br>
The plugin interacts with TubeArchivist APIs to fetch videos and channels metadata.

### Features
- Add metadata for videos (episodes)
- Add metadata for channels (shows)
- Add images for videos (episodes), ie. thumb images
- Add images for channels (shows), ie. thumb, tvart and banner images
- Organize videos (episodes) by year (seasons)

## Installation
### From official repository (recommended)
1. Go to `Dashboard -> Plugins` and select the `Repositories` tab
2. Add a new repository with the following details:
- Repository name: `TubeArchivistMetadata`
- Repository URL: `https://github.com/tubearchivist/tubearchivist-jf-plugin/raw/master/manifest.json`
  ![Add repository](https://github.com/tubearchivist/tubearchivist-jf-plugin/assets/31162436/b0216b21-79c4-445b-8cf1-fbf6b138dee0)

3. Go to the `Catalog` tab
4. Find `TubeArchivistMetadata` in the `Metadata` section and install it
![Find plugin](https://github.com/tubearchivist/tubearchivist-jf-plugin/assets/31162436/a30a14c2-33cd-44c1-b96a-406662726e5e)
5. Restart Jellyfin to apply the changes

### From ZIP in GitHub releases
1. Download the latest available release (`tubearchivistmetadata_*.zip`) from the repository releases section
2. Extract the contained files in the `plugins/TubeArchivistMetadata` folder (you might need to create the folders) of your Jellyfin installation
3. Restart Jellyfin to apply the changes

## Configuration
<p>This plugin requires that you have already an instance of TubeArchivist up and running.</p>
Once installed, you have to configure the following parameters in the plugin configuration:
<ul>
    <li>Collection display name</li>
    <li>TubeArchivist instance address</li>
    <li>TubeArchivist API key</li>
    <li>Overviews length (channels and videos descriptions)</li>
    <li>Playback synchronization settings discussed in the <a href="#playback-synchronization">Playback synchronization</a> paragraph</li>
</ul>

![Plugin configuration](https://github.com/tubearchivist/tubearchivist-jf-plugin/assets/31162436/d34464ea-ddfb-44b3-9d3e-5d5974956c58)


## Use the plugin
<p>Using the plugin is very simple. Let's start from the beginning:</p>

_NOTE: If you are using Docker containers, it is important to mount the TubeArchivist media path into Jellyfin container as **read-only**, in order to avoid possible operations on the media files that will break TubeArchivist._ <br>
1. Go to `Dashboard -> Libraries` and add a media library
2. In the form select `Shows` as Content type, set a display name for the library and set the TubeArchivist media folder in the `Folders` section
![Add library](https://github.com/tubearchivist/tubearchivist-jf-plugin/assets/31162436/1eca534e-0929-4134-8587-3cff0009f618)

4. Scrolling down, uncheck all metadata and image providers except `TubeArchivist`. (You won't find TubeArchivist in seasons providers, so just disable everything there)
5. Save and come back to Home, you will see the newly added library. Jellyfin will have executed the metadata fetching for you after the collection creation and then you will see the metadata and the images of channels and videos


## Playback synchronization
<p>Starting from v1.3.1 this plugin offers playback progress and watched status bidirectional synchronization, but you can choose to enable only a one way synchronization (Jellyfin->TubeArchivist or TubeArchivist->Jellyfin) too.</p>

### Jellyfin->TubeArchivist synchronization
<p>This kind of synchronization is done listening for progress and watched status changes while playing the videos for the specified users.<br>Furthermore, there is a task that runs at Jellyfin startup to synchronize the whole library.</p>
<p>In the plugin configuration you will find these settings:</p>

![JF->TA playback synchronization settings](https://github.com/user-attachments/assets/dc6be82f-e685-4896-a502-317681c47fc7)
<p>In the text field you can specify one Jellyfin username to synchronize data of to TubeArchivist.</p>

### TubeArchivist->Jellyfin synchronization
<p>This kind of synchronization is done using a Jellyfin scheduled task that regularly synchronizes data from TubeArchivist API to Jellyfin.</p>
<p>In the plugin configuration you will find these settings:</p>

![TA->JF playback synchronization settings](https://github.com/user-attachments/assets/1b4c33af-834f-45b3-9057-71830e7c8b4f)
<p>In the first text field you can specify one or more Jellyfin usernames to update data for.<br>
In the second field you can specify the interval in seconds the task should run at, so that you can choose according to your system requirements. The lower is the interval the higher will be the resources consuption on your system.</p>

## Episode numbering

<p>There are different ways to number the episodes as they are configured in Jellyfin.<br>
This changes the number after E in S--E-- (for example S2024E100 for episode number 100 of season 2024).</p>

![Episode numbering scheme options](https://github.com/user-attachments/assets/6d36bc2c-ca9d-4a5c-8021-e15d399316fc)

The options correlate with:
- Default - leave the numbering to what Jellyfin does by default (this is what the plugin has always done)
- YYYYMMDD - numbers the episode by the year, month, day (e.g. 20250804 for a video published on the 4th of August 2025)

## Build

1. To build this plugin you will need [.Net 8.x](https://dotnet.microsoft.com/download/dotnet/8.0).

2. Build plugin with following command
  ```
  $ dotnet publish Jellyfin.Plugin.TubeArchivistMetadata --configuration Release --output bin
  ```

3. Place the dll-file in the `plugins/tubearchivist-jf-plugin` folder (you might need to create the folders) of your Jellyfin installation

## License

This plugins code and packages are distributed under the GPLv3 License. See [LICENSE](./LICENSE) for more information.
