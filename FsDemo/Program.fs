// For more information see https://aka.ms/fsharp-console-apps
let True a _ = a
let False _ b = b

let IfElse cond thenBranch elseBranch =
    cond thenBranch elseBranch

let Not cond =
    IfElse cond False True
let And a b =
    IfElse a (IfElse b True False) False
let Or a b =
    IfElse a True (IfElse b True False)
let Xor a b =
    IfElse a (IfElse b False True) (IfElse b True False)
let Imply a b =
    IfElse a (IfElse b True False) True

let Res = IfElse True 1 2
printfn "%d" Res


