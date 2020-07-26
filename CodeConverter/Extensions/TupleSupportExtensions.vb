﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Text

Imports Microsoft.CodeAnalysis

Imports VBFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory
Imports VBS = Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace CSharpToVBCodeConverter.ToVisualBasic

    Public Module TupleSupportExtensions

        ''' <summary>
        ''' Extract String
        ''' </summary>
        ''' <param name="CSNamedTypeString">Source String</param>
        ''' <param name="StartIndex">Start of Substring</param>
        ''' <param name="PossibleIndex">End Index or -1 if no end</param>
        ''' <returns>Substring from i to CommaIndex or end if CommanIndex = -1</returns>
        <Extension>
        Private Function ExtractName(CSNamedTypeString As String, StartIndex As Integer, PossibleIndex As Integer) As String
            Dim length As Integer
            If PossibleIndex < 0 Then
                length = CSNamedTypeString.Length - StartIndex - 1
            Else
                length = PossibleIndex - StartIndex
            End If
            If length <= 0 Then
                Return ""
            End If
            Return MakeVBSafeName(CSNamedTypeString.Substring(StartIndex, length).Trim)
        End Function

        <Extension>
        Friend Function ConvertCSTupleToVBType(CSNamedType As ITypeSymbol) As VBS.TypeSyntax
            Return ConvertCSTupleToVBType(CSNamedType.ToString)
        End Function

        <Extension>
        Friend Function ConvertCSTupleToVBType(CSNamedTypeStringIn As String) As VBS.TypeSyntax
            Dim CSNamedTypeString As String = CSNamedTypeStringIn
            Dim IsArray As Boolean = False
            Dim Nullable As Boolean = False
            If CSNamedTypeString.EndsWith("?", StringComparison.Ordinal) Then
                Nullable = True
                CSNamedTypeString = CSNamedTypeString.Substring(0, CSNamedTypeString.Length - 1).Trim
            End If
            If CSNamedTypeString.EndsWith("[]", StringComparison.Ordinal) Then
                IsArray = True
                CSNamedTypeString = CSNamedTypeString.Substring(0, CSNamedTypeString.Length - 2).Trim
            End If
            If CSNamedTypeString.StartsWith("(", StringComparison.OrdinalIgnoreCase) AndAlso CSNamedTypeString.EndsWith(")", StringComparison.OrdinalIgnoreCase) Then
                CSNamedTypeString = CSNamedTypeString.Substring(1, CSNamedTypeString.Length - 2).Trim
            End If

            Dim ElementList As List(Of String) = CSNamedTypeString.ConvertTupleToVBTypeStrings(IncludeName:=True)
            Dim builder As New StringBuilder
            builder.Append("(")
            For Each e As IndexClass(Of String) In ElementList.WithIndex
                If e.IsLast Then Exit For
                builder.Append($"{e.Value}, ")
            Next
            builder.Append(ElementList.Last & ")")
            Dim TupleType As String = builder.ToString & If(IsArray, "()", "") & If(Nullable, "?", "")

            Return VBFactory.ParseTypeName(TupleType).WithLeadingTrivia(SpaceTrivia)
        End Function

        <Extension>
        Friend Function ConvertToTupleElement(TupleElement As IFieldSymbol) As VBS.TupleElementSyntax
            If TupleElement.Type Is Nothing Then
                Return VBFactory.NamedTupleElement(TupleElement.Name.ToString(Globalization.CultureInfo.InvariantCulture))
            End If
            Dim AsClause As VBS.SimpleAsClauseSyntax = VBFactory.SimpleAsClause(TupleElement.Type.ConvertToType())
            Return VBFactory.NamedTupleElement(VBFactory.Identifier(MakeVBSafeName(TupleElement.Name)), AsClause)
        End Function

        <Extension>
        Friend Function ConvertTupleToVBTypeStrings(CSharpNamedTypeString As String, IncludeName As Boolean) As List(Of String)
            Dim currentChar As String
            Dim openLT As Integer
            Dim openParen As Integer
            Dim tmpString As New StringBuilder
            Dim ElementList As New List(Of String)

            For currentIndex As Integer = 0 To CSharpNamedTypeString.Length - 1
                currentChar = CSharpNamedTypeString(currentIndex)
                Select Case currentChar
                    Case "<"
                        openLT = 1
                        tmpString.Append(currentChar)
                        While openLT <> 0
                            currentIndex += 1
                            currentChar = CSharpNamedTypeString(currentIndex)
                            Select Case currentChar
                                Case ">"
                                    openLT -= 1
                                    tmpString.Append(">")
                                    If openLT = 0 Then
                                        Exit While
                                    End If
                                Case "<"
                                    openLT += 1
                                    tmpString.Append("<")
                                Case "["
                                    tmpString.Append("(")
                                Case "]"
                                    tmpString.Append(")")
                                Case Else
                                    tmpString.Append(currentChar)
                            End Select
                        End While
                        currentIndex += 1
                        If currentIndex >= CSharpNamedTypeString.Length - 1 OrElse Not ".[".Contains(CSharpNamedTypeString(currentIndex), StringComparison.OrdinalIgnoreCase) Then
                            Dim commaIndex As Integer = CSharpNamedTypeString.IndexOf(",", currentIndex, StringComparison.OrdinalIgnoreCase)
                            If IncludeName Then
                                Dim name As String = CSharpNamedTypeString.ExtractName(currentIndex, commaIndex)
                                If name.Length = 0 Then
                                    ElementList.Add($"{ConvertToType(tmpString.ToString)}")
                                Else
                                    ElementList.Add($"{MakeVBSafeName(name)} As {ConvertToType(tmpString.ToString)}")
                                End If
                                If commaIndex < 0 Then
                                    tmpString.Clear()
                                    Exit For
                                End If
                            Else
                                ElementList.Add(ConvertToType(tmpString.ToString).ToString)
                            End If
                            tmpString.Clear()
                            currentIndex = commaIndex + 1
                        End If
                    Case " " ' variable name
                        Dim commaIndex As Integer = CSharpNamedTypeString.IndexOf(",", currentIndex + 1, StringComparison.OrdinalIgnoreCase)
                        Dim TypePart As String = ConvertToType(tmpString.ToString).ToString
                        If IncludeName Then
                            currentIndex += 1
                            Dim name As String = CSharpNamedTypeString.ExtractName(currentIndex, commaIndex)
                            ElementList.Add($"{MakeVBSafeName(name)} As {TypePart}")
                        Else
                            ElementList.Add(TypePart)
                        End If
                        tmpString.Clear()
                        If commaIndex < 0 Then
                            Exit For
                        End If
                        currentIndex = commaIndex + 1
                    Case ","
                        ElementList.Add(ConvertToType(tmpString.ToString).ToString)
                        tmpString.Clear()
                        currentIndex += If(CSharpNamedTypeString(currentIndex + 1) = " ", 1, 0)
                    Case ")"
                        Dim TypePart As String = ConvertToType(tmpString.ToString).ToString
                        Dim commaIndex As Integer = CSharpNamedTypeString.IndexOf(",", currentIndex + 1, StringComparison.OrdinalIgnoreCase)
                        If IncludeName Then
                            currentIndex += 1
                            Dim name As String = CSharpNamedTypeString.ExtractName(currentIndex, commaIndex)
                            If commaIndex < 0 Then
                                ElementList.Add($"{MakeVBSafeName(name)} As {TypePart}")
                                Exit For
                            End If
                        Else
                            ElementList.Add(TypePart)
                        End If
                        Exit For
                    Case "?"
                        ElementList.Add("Object")
                        tmpString.Clear()
                        Dim commaIndex As Integer = CSharpNamedTypeString.IndexOf(",", currentIndex + 1, StringComparison.OrdinalIgnoreCase)
                        If commaIndex < 0 Then
                            Exit For
                        End If
                        currentIndex = commaIndex + 1
                    Case "("
                        If tmpString.Length <> 0 Then
                            Stop
                        End If
                        openParen += 1
                        tmpString.Append("(")
                        While openParen <> 0
                            currentIndex += 1
                            currentChar = CSharpNamedTypeString(currentIndex)
                            Select Case currentChar
                                Case ")"
                                    openParen -= 1
                                    tmpString.Append(")")
                                    If openParen = 0 Then
                                        Exit While
                                    End If
                                Case "("
                                    openParen += 1
                                    tmpString.Append("(")
                                Case Else
                                    tmpString.Append(currentChar)
                            End Select
                        End While
                        Dim subTupleList As New List(Of String)
                        subTupleList.AddRange(tmpString.ToString.Substring(1, tmpString.Length - 2).ConvertTupleToVBTypeStrings(IncludeName))
                        ElementList.Add($"({subTupleList.ToArray.JoinLines(", ")})")
                        tmpString.Clear()
                        Dim commaIndex As Integer = CSharpNamedTypeString.IndexOf(",", currentIndex + 1, StringComparison.OrdinalIgnoreCase)
                        If commaIndex < 0 Then
                            Exit For
                        End If
                        If commaIndex = -1 Then
                            Exit For
                        End If
                        currentIndex = commaIndex + 1
                    Case Else
                        tmpString.Append(currentChar)
                End Select
            Next
            If tmpString.Length > 0 Then
                Dim TypePart As String = ConvertToType(tmpString.ToString).ToString
                ElementList.Add(TypePart)
            End If
            Return ElementList
        End Function

        <Extension>
        Friend Function ExtractConvertedTuple(TupleString As String) As String
            Dim TupleElements As New List(Of String)
            For Each t As String In TupleString.Substring(1, TupleString.Length - 2).Split(","c)
                Dim TuplePart() As String = t.Trim.Split(" "c)
                If TuplePart.Length = 1 Then
                    TupleElements.Add(ConvertToType(TuplePart(0).ToString(Globalization.CultureInfo.InvariantCulture)).ToString)
                Else
                    Dim Identifier As SyntaxToken = CSharp.SyntaxFactory.Identifier(TuplePart(1))
                    TupleElements.Add($"{GenerateSafeVBToken(Identifier).ValueText} As {ConvertToType(TuplePart(0).ToString(Globalization.CultureInfo.InvariantCulture))}")
                End If
            Next
            Return $"({String.Join(", ", TupleElements)})"
        End Function

    End Module

End Namespace