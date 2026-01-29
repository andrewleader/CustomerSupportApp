# Contoso.AI.PolitenessAnalysis

AI-powered politeness analysis for text using ONNX Runtime and a BERT-based model.

## Features

- Analyzes text for politeness level (Polite, Somewhat Polite, Neutral, Impolite)
- Performance and Efficiency modes for different hardware configurations
- Async API for non-blocking operations
- Automatic model download at build time

## Installation

```bash
dotnet add package Contoso.AI.PolitenessAnalysis
```

## Model Download

**Important**: This package uses a ~418 MB ONNX model that is **automatically downloaded at build time**.

- The model is downloaded to `Models/polite-guard-model.onnx` in your project directory
- Download happens only once (cached for subsequent builds)
- The model file is automatically copied to your output directory
- Add `Models/*.onnx` to your `.gitignore` to avoid committing the large file

## Usage

```csharp
using Contoso.AI.PolitenessAnalysis;

// Initialize (call once at startup)
var readyResult = await PolitenessAnalyzer.EnsureReadyAsync();
if (readyResult.Status != AIFeatureReadyResultState.Success)
{
    // Handle initialization failure
    return;
}

// Create analyzer instance
var analyzer = await PolitenessAnalyzer.CreateAsync(PerformanceMode.Performance);

// Analyze text
var result = await analyzer.AnalyzeAsync("Thank you so much for your help!");

Console.WriteLine($"Politeness Level: {result.Level}");
Console.WriteLine($"Description: {result.Description}");
Console.WriteLine($"Inference Time: {result.InferenceTimeMs}ms");

// Don't forget to dispose
analyzer.Dispose();
```

## Performance Modes

- `PerformanceMode.Performance` - Optimized for speed (uses GPU/NPU if available)
- `PerformanceMode.Efficiency` - Optimized for power efficiency (uses CPU)

## Model Information

- **Source**: [Intel/polite-guard on HuggingFace](https://huggingface.co/Intel/polite-guard)
- **Size**: ~418 MB
- **License**: Apache 2.0
- **Type**: BERT-based ONNX model

## Requirements

- .NET 8.0 or later
- Windows 10 SDK 19041 or later
- Internet connection for initial model download

## License

MIT
