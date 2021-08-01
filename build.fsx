#r "nuget: FSharp.Formatting"
open System.IO
open FSharp.Formatting.Literate
open FSharp.Formatting.Literate.Evaluation

let eval = FsiEvaluator()

// Return path relative to the current file location
let relative subdir = Path.Combine(__SOURCE_DIRECTORY__, subdir)

let processFile outputDirectory file =
  let fileInfo = FileInfo(file)
  let name = Path.GetFileNameWithoutExtension(fileInfo.Name)
  let tempFile = Path.Combine(outputDirectory, name + ".temp")
  let htmlFile = Path.Combine(outputDirectory, name + ".html")
  let targetFile = FileInfo(htmlFile)
  if not targetFile.Exists || targetFile.LastWriteTime < fileInfo.LastWriteTime then
    Literate.ConvertScriptFile(input = fileInfo.FullName, output = tempFile, fsiEvaluator = eval)
    let frontMatter = [ "---"
                        "layout: post"
                        "---" ]
    File.WriteAllLines(htmlFile, Seq.append frontMatter (File.ReadLines tempFile))
    File.Delete(tempFile)
    printfn "Processed %s." name


let processDirectory path =
  Directory.EnumerateFiles path
  |> Seq.iter (processFile (relative "_posts"))

processDirectory (relative "fsx")
