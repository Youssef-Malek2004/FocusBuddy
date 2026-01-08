#!/usr/bin/env swift

import Vision
import CoreImage
import Foundation

// Read image path from command line
guard CommandLine.arguments.count > 1 else {
    print("Usage: VisionOCR.swift <image_path>")
    exit(1)
}

let imagePath = CommandLine.arguments[1]

// Load image
guard let image = CIImage(contentsOf: URL(fileURLWithPath: imagePath)) else {
    print("ERROR: Failed to load image")
    exit(1)
}

// Create text recognition request
let request = VNRecognizeTextRequest()
request.recognitionLevel = .accurate
request.usesLanguageCorrection = true

// Process the image
let handler = VNImageRequestHandler(ciImage: image, options: [:])

do {
    try handler.perform([request])

    guard let observations = request.results else {
        print("")
        exit(0)
    }

    // Extract all recognized text
    var allText: [String] = []
    for observation in observations {
        guard let topCandidate = observation.topCandidates(1).first else { continue }
        allText.append(topCandidate.string)
    }

    // Output all text, one per line
    print(allText.joined(separator: "\n"))

} catch {
    print("ERROR: \(error.localizedDescription)")
    exit(1)
}
