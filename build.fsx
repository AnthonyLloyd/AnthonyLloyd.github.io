#r "packages/FAKE/tools/FakeLib.dll"

open Fake

Target "Posts" (fun _ ->
    ["./_posts/"] |> CleanDirs
    FSharpFormatting.run "literate --processDirectory --inputDirectory posts --outputDirectory _posts --noLineNumbers"
    !!"./_posts/*" |> ReplaceInFiles ["<p>---","---";"---</p>","---"]
)

RunTargetOrDefault "Posts"