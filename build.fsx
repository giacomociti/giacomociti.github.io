#load "./packages/FSharp.Formatting/FSharp.Formatting.fsx"
open System.IO
open FSharp.Literate

// Return path relative to the current file location
let relative subdir = Path.Combine(__SOURCE_DIRECTORY__, subdir)

Literate.ProcessDirectory(
  inputDirectory = relative "fsx", 
  templateFile = relative "template.html",
  lineNumbers = false,
  outputDirectory = relative "_posts")