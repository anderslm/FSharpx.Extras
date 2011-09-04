﻿module FSharp.Monad.Tests.BinaryTest

open System
open FSharp.Collections
open FSharp.Collections.ByteString
open FSharp.Monad.Iteratee
open FSharp.Monad.Iteratee.Binary
open FSharp.Monad.Iteratee.Operators
open NUnit.Framework
open FsUnit

let runTest i =
  match run i with
  | Choice1Of2 e -> raise e
  | Choice2Of2 x -> x

[<Test>]
let ``test length should calculate the length of the list without modification``() =
  let actual = enumerate (create [|1uy;2uy;3uy|]) length |> runTest 
  actual |> should equal 3

[<Test>]
let ``test length should calculate the length of the list without modification when enumerated one byte at a time``() =
  let actual = enumeratePureNChunk 1 (create [|1uy;2uy;3uy|]) length |> runTest 
  actual |> should equal 3

let testPeekAndHead = [|
  [| box empty; box None |]
  [| box (singleton 'c'B); box (Some 'c'B) |]
  [| box (create "char"B); box (Some 'c'B) |]
|]

[<Test>]
[<TestCaseSource("testPeekAndHead")>]
let ``test peek should return the value without removing it from the stream``(input, expected:byte option) =
  let actual = enumerate input peek |> runTest 
  actual |> should equal expected

[<Test>]
[<TestCaseSource("testPeekAndHead")>]
let ``test peek should return the value without removing it from the stream when enumerated one byte at a time``(input, expected:byte option) =
  let actual = enumeratePureNChunk 1 input peek |> runTest 
  actual |> should equal expected

[<Test>]
[<TestCaseSource("testPeekAndHead")>]
let ``test head should return the value and remove it from the stream``(input, expected:byte option) =
  let actual = enumerate input head |> runTest
  actual |> should equal expected

[<Test>]
[<TestCaseSource("testPeekAndHead")>]
let ``test head should return the value and remove it from the stream when enumerated one byte at a time``(input, expected:byte option) =
  let actual = enumeratePureNChunk 1 input head |> runTest
  actual |> should equal expected

[<Test>]
[<Sequential>]
let ``test drop should drop the first n items``([<Values(0,1,2,3,4,5,6,7,8,9)>] x) =
  let drop2Head = iteratee {
    do! drop x
    return! head }
  let actual = enumerate (create [| 0uy..9uy |]) drop2Head |> runTest
  actual |> should equal (Some(byte x))

[<Ignore("The enumerator appears to be caching earlier values and re-applying them, rendering an invalid result.")>]
[<Test>]
[<Sequential>]
let ``test drop should drop the first n items when enumerating in chunks``([<Values(0,1,2,3,4,5,6,7,8,9)>] x) =
  let drop2Head = iteratee {
    do! drop x
    return! head }
  let actual = enumeratePureNChunk 5 (create [| 0uy..9uy |]) drop2Head |> runTest
  actual |> should equal (Some(byte x))

[<Test>]
let ``test dropWhile should drop anything before the first space``() =
  let dropWhile2Head = iteratee {
    do! dropWhile ((<>) ' 'B)
    return! head }
  let actual = enumerate (create "Hello world"B) dropWhile2Head |> runTest
  actual |> should equal (Some ' 'B)

[<Test>]
let ``test dropWhile should drop anything before the first space when enumerating in chunks``() =
  let dropWhile2Head = iteratee {
    do! dropWhile ((<>) ' 'B)
    return! head }
  let actual = enumeratePureNChunk 5 (create "Hello world"B) dropWhile2Head |> runTest
  actual |> should equal (Some ' 'B)

[<Test>]
[<Sequential>]
let ``test take should take the first n items``([<Values(0,1,2,3,4,5,6,7,8,9,10)>] x) =
  let input = create [|0uy..9uy|]
  let actual = enumerate input (take x) |> runTest
  Assert.That(actual == (ByteString.take x input))

[<Test>]
[<Sequential>]
let ``test take should take the first n items when enumerating in chunks``([<Values(0,1,2,3,4,5,6,7,8,9,10)>] x) =
  let input = create [|0uy..9uy|]
  let actual = enumeratePureNChunk 5 input (take x) |> runTest
  Assert.That(actual == (ByteString.take x input))

[<Test>]
let ``test takeWhile should take anything before the first space``() =
  let input = "Hello world"B
  let actual = enumeratePure1Chunk (create input) (takeWhile ((<>) ' 'B)) |> runTest
  Assert.That(actual == (BS(input, 0, 5)))

[<Ignore("The enumerator appears to be caching earlier values and re-applying them, rendering an invalid result.")>]
[<Test>]
let ``test takeWhile should take anything before the first space when enumerating in chunks``() =
  let input = "Hello world"B
  let actual = enumeratePureNChunk 5 (create input) (takeWhile ((<>) ' 'B)) |> runTest
  Assert.That(actual == (BS(input, 0, 5)))

[<Test>]
let ``test takeUntil should correctly split the input``() =
  let input = "abcde"B
  let actual = enumeratePure1Chunk (create input) (takeUntil ((=) 'c'B)) |> runTest
  Assert.That(actual == (BS(input, 0, 2)))

[<Ignore("The enumerator appears to be caching earlier values and re-applying them, rendering an invalid result.")>]
[<Test>]
let ``test takeUntil should correctly split the input when enumerating in chunks``() =
  let input = "abcde"B
  let actual = enumeratePureNChunk 2 (create input) (takeUntil ((=) 'c'B)) |> runTest
  Assert.That(actual == (BS(input, 0, 2)))

let takeUntilTests = [|
  [| box ""B; box empty; box empty |]
  [| box "\r"B; box empty; box (singleton '\r'B) |]
  [| box "\n"B; box empty; box (singleton '\n'B) |]
  [| box "\r\n"B; box empty; box (create "\r\n"B) |]
  [| box "line1"B; box empty; box empty |]
  [| box "line1\n"B; box (create "line1"B); box (singleton '\n'B) |]
  [| box "line1\r"B; box (create "line1"B); box (singleton '\r'B) |]
  [| box "line1\r\n"B; box (create "line1"B); box (create "\r\n"B) |]
|]

[<Test>]
[<TestCaseSource("takeUntilTests")>]
let ``test takeUntilNewline should split strings on a newline character``(input, expectedRes:ArraySegment<byte>, expectedRem:ArraySegment<byte>) =
  let isNewline c = c = '\r'B || c = '\n'B
  let res, rem =
    match enumeratePure1Chunk (create input) (takeUntil isNewline) with
    | Yield(res, (Chunk rem)) -> res, rem
    | Continue _ -> empty, empty
    | _ -> failwith "Unrecognized test result"
  Assert.That(res == expectedRes)
  Assert.That(rem == expectedRem)

[<Test>]
let ``test heads should count the number of characters in a set of headers``() =
  let actual = enumeratePure1Chunk (ByteString.ofString "abd") (heads (ByteString.ofString "abc")) |> runTest
  actual |> should equal 2

[<Test>]
let ``test heads should count the number of characters in a set of headers when enumerated one byte at a time``() =
  let actual = enumerate (ByteString.ofString "abd") (heads (ByteString.ofString "abc")) |> runTest
  actual |> should equal 2

[<Test>]
let ``test heads should count the number of characters in a set of headers when enumerating in chunks``() =
  let actual = enumeratePureNChunk 2 (ByteString.ofString "abd") (heads (ByteString.ofString "abc")) |> runTest
  actual |> should equal 2

[<Test>]
let ``test heads should count the correct number of newline characters in a set of headers when enumerated one byte at a time``() =
  let isNewline c = c = '\r'B || c = '\n'B
  let readUntilNewline = takeUntil isNewline >>= fun bs -> heads (create "\r\n"B)
  let actual = enumerate (ByteString.ofString "abc\r\n") readUntilNewline |> runTest
  actual |> should equal 2

let readLinesTests = [|
  [| box ""B; box (Choice1Of2 []:Choice<String list, String list>) |]
  [| box "\r"B; box (Choice2Of2 []:Choice<String list, String list>) |]
  [| box "\n"B; box (Choice2Of2 []:Choice<String list, String list>) |]
  [| box "\r\n"B; box (Choice2Of2 []:Choice<String list, String list>) |]
  [| box "line1"B; box (Choice1Of2 []:Choice<String list, String list>) |]
  [| box "line1\n"B; box (Choice1Of2 ["line1"]:Choice<String list, String list>) |]
  [| box "line1\r"B; box (Choice1Of2 ["line1"]:Choice<String list, String list>) |]
  [| box "line1\r\n"B; box (Choice1Of2 ["line1"]:Choice<String list, String list>) |]
  [| box "line1\r\nline2"B; box (Choice1Of2 ["line1"]:Choice<String list, String list>) |]
  [| box "line1\r\nline2\r\n"B; box (Choice1Of2 ["line1";"line2"]:Choice<String list, String list>) |]
  [| box "line1\r\nline2\r\n\r\n"B; box (Choice2Of2 ["line1";"line2"]:Choice<String list, String list>) |]
  [| box "line1\r\nline2\r\nline3\r\nline4\r\nline5"B; box (Choice1Of2 ["line1";"line2";"line3";"line4"]:Choice<String list, String list>) |]
  [| box "line1\r\nline2\r\nline3\r\nline4\r\nline5\r\n"B
     box (Choice1Of2 ["line1";"line2";"line3";"line4";"line5"]:Choice<String list, String list>) |]
  [| box "PUT /file HTTP/1.1\r\nHost: example.com\rUser-Agent: X\nContent-Type: text/plain\r\n\r\n1C\r\nbody line 2\r\n\r\n7"B
     box (Choice2Of2 ["PUT /file HTTP/1.1";"Host: example.com";"User-Agent: X";"Content-Type: text/plain"]:Choice<String list, String list>) |]
|]

[<Test>]
[<TestCaseSource("readLinesTests")>]
let ``test readLines should return the lines from the input``(input, expected:Choice<String list, String list>) =
  let actual = enumeratePure1Chunk (create input) readLines |> runTest
  actual |> should equal expected

[<Ignore("heads and readLines do not correctly return a correct result when the input is chunked and a \r\n is encountered in different chunks.")>]
[<Test>]
[<TestCaseSource("readLinesTests")>]
let ``test readLines should return the lines from the input when enumerated one byte at a time``(input, expected:Choice<String list, String list>) =
  let actual = enumerate (create input) readLines |> runTest
  actual |> should equal expected

[<Ignore("The enumerator appears to be caching earlier values and re-applying them, rendering an invalid result.")>]
[<Test>]
[<TestCaseSource("readLinesTests")>]
let ``test readLines should return the lines from the input when chunked``(input, expected:Choice<String list, String list>) =
  let actual = enumeratePureNChunk 11 (* Problem is that this is not consistent; try 5 and 10 *) (create input) readLines |> runTest
  actual |> should equal expected