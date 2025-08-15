﻿# OpenXmlPowerTools

[![.NET build and test](https://github.com/Codeuctivity/OpenXmlPowerTools/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Codeuctivity/OpenXmlPowerTools/actions/workflows/dotnet.yml) [![Nuget](https://img.shields.io/nuget/v/Codeuctivity.OpenXmlPowerTools.svg)](https://www.nuget.org/packages/Codeuctivity.OpenXmlPowerTools/) 

## Focus of this fork

- Linux, Windows and MacOs support was added by this fork
- Conversion of DOCX to HTML/CSS

## Known missing features - Conversion of DOCX to HTML/CSS

- [floating settings of images are ignored](https://stackoverflow.com/questions/70277539/how-to-handle-floating-images-in-openxml-and-convert-to-html-equivalent/73639409#73639409)
- [W.oMath](https://github.com/Codeuctivity/OpenXmlToHtml/issues/74)
- many more


## Example - Convert DOCX to HTML

``` csharp
var sourceDocxFileContent = File.ReadAllBytes("./source.docx");
using var memoryStream = new MemoryStream();
await memoryStream.WriteAsync(sourceDocxFileContent, 0, sourceDocxFileContent.Length);
using var wordProcessingDocument = WordprocessingDocument.Open(memoryStream, true);
var settings = new WmlToHtmlConverterSettings("htmlPageTitle");
var html = WmlToHtmlConverter.ConvertToHtml(wordProcessingDocument, settings);
var htmlString = html.ToString(SaveOptions.DisableFormatting);
File.WriteAllText("./target.html", htmlString, Encoding.UTF8);
```

### Other features

- Splitting DOCX/PPTX files into multiple files.
- Combining multiple DOCX files into a single file.
- Populating content in template DOCX files with data from XML.
- Conversion of HTML/CSS to DOCX.
- Searching and replacing content in DOCX/PPTX using regular expressions.
- Managing tracked-revisions, including detecting tracked revisions, and accepting tracked revisions.
- Updating Charts in DOCX/PPTX files, including updating cached data, as well as the embedded XLSX.
- Retrieving metrics from DOCX files, including the hierarchy of styles used, the languages used, and the fonts used.
- Writing XLSX files using far simpler code than directly writing the markup, including a streaming approach that
  enables writing XLSX files with millions of rows.
- Extracting data (along with formatting) from spreadsheets.

## SkiaSharp migration

Earlier releases used the ImageSharp library for tasks such as decoding images in the WordprocessingML to HTML converter and validating rendering output in tests. ImageSharp's powerful API came with a more restrictive licensing model that could require commercial agreements, which proved limiting for downstream projects.

The project now uses SkiaSharp to handle these responsibilities. SkiaSharp, distributed under the permissive MIT license, provides cross-platform bindings to the Skia graphics engine. By leveraging SkiaSharp's `SKCodec` and `SKImage` APIs for image transformation and `SKColor` for color parsing, the codebase avoids licensing friction while retaining rich imaging capabilities.

## Development

- Run `dotnet build OpenXmlPowerTools.sln`
