# Weather Service Transport library

This project builds a library to retrieve weather forecasts from the [National Weather Service API](https://www.weather.gov/documentation/services-web-api). As part of this, it generates a client SDK for connecting to the remote service, trimmed down to just the subset needed for this sample.

## `openapi.yaml`

The Open API definition is reduced to only the `/gridpoints/{wfo}/{x},{y}/forecast` endpoint, with all referenced components.

## `nswag.json`

The NSwag definition file describes what options NSwag should use when generating the client SDK based on the Open API.

## `ApiClientBase.cs`

This partial class definition modifies the generated requests to include a User-Agent header, as required by NWS.

## `obj/ApiClient.cs`

The client SDK itself is generated at build time by NSwag, and put into this file. That is then included in compilation.

## `WeatherApiClient.csproj`

The client SDK generation is specified in the project definition file.

```xml

  <PropertyGroup>
    <ApiClientConfigFile>nswag.json</ApiClientConfigFile>
    <ApiClientInputFile>openapi.yaml</ApiClientInputFile>
    <ApiClientOutputFile>$(BaseIntermediateOutputPath)\ApiClient.cs</ApiClientOutputFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NSwag.MSBuild" Version="14.1.0">
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <!--Custom task to generate source code from OpenApi Specification before compilation-->
  <Target Name="GenerateSources" BeforeTargets="BeforeBuild" Inputs="$(ApiClientConfigFile);$(ApiClientInputFile)" Outputs="$(ApiClientOutputFile)">
    <Exec Command="$(NSwagExe_Net80) run $(ApiClientConfigFile) /variables:OutputFile=$(ApiClientOutputFile)" ConsoleToMSBuild="true" />
  </Target>
 
  <!--Custom task to remove generated source code before clean project-->
  <Target Name="RemoveGenerateSources" BeforeTargets="CoreClean">
      <RemoveDir Directories="$(ApiClientOutputFile)" />
  </Target>

  <!--Register generated source code as project source code-->
  <ItemGroup>
    <Compile Include="$(ApiClientOutputFile)" />
  </ItemGroup>

```

## `sample/forecast.json`

This is an example of what data is sent up to the Data Collection Endpoint. If you create a Data Collection Rule by hand in the Azure portal, you will find this helpful to upload as an example of the incoming data.
