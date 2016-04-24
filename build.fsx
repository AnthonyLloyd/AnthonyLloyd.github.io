#r "packages/FAKE/tools/FakeLib.dll"

open Fake

Target "Posts" (fun _ ->
    FSharpFormatting.run "literate --processDirectory --inputDirectory  posts --outputDirectory _posts --lineNumbers false" 
)

RunTargetOrDefault "Posts"