(**
### XSD is dead, long live XSD!
My little contribution to the F# OSS ecosystem is schema support for the
XML Type Provider. It's been recently merged into [F# Data][FSharp.Data]
(and will ship soon in the upcoming [version 3.0][FSharp.Data.beta]) after 
being available for a while as a [standalone project][FSharp.Data.Xsd].

It "comes with comprehensible documentation" but I'm going to use this blog
to post a few tips covering marginal aspects. 

Before introducing the type provider (and today's tip about 
nillable elements) let me spend a few words about schemas.
## Validation

Having a schema allows to validate documents against it.
We will use the following handy snippet:
*)
open System.Xml
open System.Xml.Schema

let createSchema (xmlReader: XmlReader) =    
    let schemaSet = XmlSchemaSet() 
    schemaSet.Add(null, xmlReader) |> ignore
    schemaSet.Compile()
    schemaSet

let parseSchema xsdText =
    use reader = XmlReader.Create(new System.IO.StringReader(xsdText))
    createSchema reader

let loadSchema xsdFile =    
    use reader = XmlReader.Create(inputUri = xsdFile)
    createSchema reader

let validator schemaSet xml =
    let settings = XmlReaderSettings(
                    ValidationType = ValidationType.Schema,
                    Schemas = schemaSet)
    use reader = XmlReader.Create(new System.IO.StringReader(xml), settings)
    try
        while reader.Read() do ()
        Result.Ok ()
    with :? XmlSchemaException as e ->
        Result.Error e.Message

(**
Given a schema (`AuthorXsd`) and some documents (`xml1` and `xml2`):
*)
[<Literal>]
let AuthorXsd = """
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" 
  elementFormDefault="qualified" attributeFormDefault="unqualified">
    <xs:element name="author" type="authorType" />
    <xs:complexType name="authorType">
        <xs:sequence>
          <xs:element name="name" type="xs:string" />
          <xs:element name="born" type="xs:int" nillable="true" />
        </xs:sequence>
    </xs:complexType>
</xs:schema>"""

let xml1 = """
<author>
    <name>Karl Popper</name>
    <born>1902</born>
</author>"""

let xml2 = """
<author>
    <born>1902</born>
</author>"""
(**
we can check their validity (and see that `xml2` lacks the `name` element):
*)
let validateAuthor = AuthorXsd |> parseSchema |> validator

(*** define-output:validate1 ***)
validateAuthor xml1
(*** include-it:validate1 ***)

(*** define-output:validate2 ***)
validateAuthor xml2
(*** include-it:validate2 ***)

(*** hide ***)
#r "System.Xml.Linq"
#r "../packages/FSharp.Data.Xsd/lib/net45/FSharp.Data.Xsd.dll"

(**
## Type Provider
The XML Type Provider can be used with the `Schema` parameter,
generating a type with `Name` and `Born` properties.
*)
open FSharp.Data

type AuthorXsd = XmlProvider<Schema=AuthorXsd>

let author = AuthorXsd.Parse xml1
printfn "%A" (author.Name, author.Born)

(**
Beware that no validation is performed, in fact also `xml2` could
be parsed, albeit accessing the `Name` property would cause an exception.
If you need to validate your input you have to do it yourself
using code like in the above validation snippet, which is useful anyway:
whenever the type provider behaves unexpectedly, first check whether the input
is valid.

You may be surprised, for example, that the following document is invalid:
*)
(*** define-output:missingBorn ***)
validateAuthor "<author><name>Karl Popper</name></author>"
(*** include-it:missingBorn ***)

(**
##Nillable Elements
The validator complains about the `born` element lacking,
although it was declared nillable.

Declaring a nillable element is a weird way to specify that its value
is not mandatory. A much simpler and more common alternative is to rely
on `minOccurs` and `maxOccurs` to specify the allowed number of elements.
But in case you stumble across a schema with nillable elements,
you need to be aware that valid documents look like this:
*)
(*** define-output:nilTrue ***)
"""
<author>
    <name>Karl Popper</name>
    <born xsi:nil="true" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" />
</author>"""
|> validateAuthor
(*** include-it:nilTrue ***)

(**
You may legitimately wonder what the heck is this strange `nil`
attribute. It belongs to a special W3C namespace and its purpose
is to explicitly signal the absence of a value.

The element tag must always be present for a nillable element!
But the element is allowed to have content only when the `nil`
attribute is false (or is simply omitted like in `xml1`):
*)

(*** define-output:nilFalse ***)
"""
<author>
    <name>Karl Popper</name>
    <born xsi:nil="false" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
        1902
    </born>
</author>"""
|> validateAuthor
(*** include-it:nilFalse ***)

(**
For nillable elements the XML Type Provider creates two
optional properties (`Nil` and `Value`).
*)
(*** define-output:printBorn ***)
printfn "%A" (author.Born.Nil, author.Born.Value)
(*** include-output:printBorn ***)

(**
For valid elements if `Nil = Some true` then `Value = None`.
The converse does not hold in general: for certain data types like
`xs:string` that admit empty content, it is possible to have `Value = None`
even if `Nil = Some false` or `Nil = None`; in fact the `nil` attribute
helps disambiguate subleties about the lack of a value: the value
was not entered *vs* the value *NULL* was entered (can you feel the smell of
the billion dollar mistake?).

In practice, when reading XML, you mostly rely on `Value` and ignore `Nil`.
When using the type provider to write XML, on the other hand, you need 
to pass appropriate values in order to obtain a valid document:
*)

(*** define-output:writeXml ***)
AuthorXsd.Author(name = "Karl Popper",
                 born = AuthorXsd.Born(nil = Some true, value = None))
|> printfn "%A"
(*** include-output:writeXml ***)

(**
[FSharp.Data]:http://fsharp.github.io/FSharp.Data/
[FSharp.Data.Xsd]:https://github.com/fsprojects/FSharp.Data.Xsd
[FSharp.Data.beta]:https://www.nuget.org/packages/FSharp.Data/3.0.0-beta4
*)