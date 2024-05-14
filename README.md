<h1 align="center">Jellyfin TubeArchivist Plugin</h1>

<p align="center">
<img alt="Plugin Banner" src="https://raw.githubusercontent.com/tubearchivist/tubearchivist-jf-plugin/master/images/logo.png"/>
<br/>
</p>

> [!IMPORTANT]
> Jellyfin 10.9.1 has been finally released and since starting with the version 1.3.0 the plugin supports only the latest Jellyfin release, in order to continue using this plugin with all the latest features you will need to upgrade your Jellyfin installation.

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

## Build

1. To build this plugin you will need [.Net 8.x](https://dotnet.microsoft.com/download/dotnet/8.0).

2. Build plugin with following command
  ```
  $ dotnet publish Jellyfin.Plugin.TubeArchivistMetadata --configuration Release --output bin
  ```

3. Place the dll-file in the `plugins/tubearchivist-jf-plugin` folder (you might need to create the folders) of your Jellyfin installation

## License

This plugins code and packages are distributed under the GPLv3 License. See [LICENSE](./LICENSE) for more information.
