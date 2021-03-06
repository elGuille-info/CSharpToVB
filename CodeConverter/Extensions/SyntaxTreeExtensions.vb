﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports System.IO
Imports System.Runtime.CompilerServices

Imports CS = Microsoft.CodeAnalysis.CSharp
Imports CSS = Microsoft.CodeAnalysis.CSharp.Syntax

Namespace CSharpToVBConverter
    Public Module SyntaxTreeExtensions
        Private ReadOnly s_autoGeneratedStrings() As String = {"< AutoGenerated", "<AutoGenerated", "<auto-generated"}

        Private Function BeginsWithAutoGeneratedComment(
        tree As SyntaxTree, isComment As Func(Of SyntaxTrivia, Boolean), CancelToken As CancellationToken) As Boolean
            Dim root As SyntaxNode = tree.GetRoot(CancelToken)
            If root.HasLeadingTrivia Then
                Dim leadingTrivia As SyntaxTriviaList = root.GetLeadingTrivia()

                For Each trivia As SyntaxTrivia In leadingTrivia
                    If Not isComment(trivia) Then
                        Continue For
                    End If

                    Dim text As String = trivia.ToString()

                    ' Check to see if the text of the comment contains an auto generated comment.
                    For Each autoGenerated As String In s_autoGeneratedStrings
                        If text.Contains(autoGenerated, StringComparison.OrdinalIgnoreCase) Then
                            Return True
                        End If
                    Next
                Next
            End If

            Return False
        End Function

        Private Function IsGeneratedCodeFile(filePath As String) As Boolean
            If Not String.IsNullOrEmpty(filePath) Then
                Dim fileName As String = Path.GetFileName(filePath)
                If fileName.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase) Then
                    Return True
                End If

                Dim extension As String = Path.GetExtension(fileName)
                If Not String.IsNullOrEmpty(extension) Then
                    Dim fileNameWithoutExtension As String = Path.GetFileNameWithoutExtension(filePath)
                    If fileNameWithoutExtension.EndsWith(".designer", StringComparison.OrdinalIgnoreCase) OrElse
                    fileNameWithoutExtension.EndsWith(".generated", StringComparison.OrdinalIgnoreCase) OrElse
                    fileNameWithoutExtension.EndsWith(".g", StringComparison.OrdinalIgnoreCase) OrElse
                    fileNameWithoutExtension.EndsWith(".g.i", StringComparison.OrdinalIgnoreCase) Then
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        Private Function IsGeneratedSymbolWithGeneratedCodeAttribute(
                                symbol As ISymbol, generatedCodeAttribute As INamedTypeSymbol) As Boolean
            Debug.Assert(symbol IsNot Nothing)
            Debug.Assert(generatedCodeAttribute IsNot Nothing)

            ' Don't check this for namespace.  Namespace's cannot have attributes on them. And,
            ' currently, calling DeclaringSyntaxReferences on an INamespaceSymbol is more expensive
            ' than is desirable.
            If symbol.Kind <> SymbolKind.Namespace Then
                ' GeneratedCodeAttribute can only be applied once on a symbol.
                ' For partial symbols with more than one definition, we must treat them as non-generated code symbols.
                If symbol.DeclaringSyntaxReferences.Length > 1 Then
                    Return False
                End If

                For Each attribute As AttributeData In symbol.GetAttributes()
                    If SymbolEqualityComparer.Default.Equals(generatedCodeAttribute, attribute.AttributeClass) Then
                        Return True
                    End If
                Next
            End If

            Return symbol.ContainingSymbol IsNot Nothing AndAlso IsGeneratedSymbolWithGeneratedCodeAttribute(symbol.ContainingSymbol, generatedCodeAttribute)
        End Function

        Private Function MatchesNamespaceOrRoot(arg As SyntaxNode) As Boolean
            Return TypeOf arg Is CSS.NamespaceDeclarationSyntax OrElse TypeOf arg Is CSS.CompilationUnitSyntax
        End Function

        <Extension>
        Friend Function HasUsingDirective(tree As CS.CSharpSyntaxTree, FullName As String) As Boolean
            If tree Is Nothing Then
                Throw New ArgumentNullException(NameOf(tree))
            End If
            If String.IsNullOrWhiteSpace(FullName) Then
                Throw New ArgumentNullException(NameOf(FullName))
            End If
            FullName = FullName.Trim()
            Return tree.GetRoot().DescendantNodes(AddressOf MatchesNamespaceOrRoot).OfType(Of CSS.UsingDirectiveSyntax).Any(Function(u As CSS.UsingDirectiveSyntax) u.Name.ToString().Equals(FullName, StringComparison.OrdinalIgnoreCase))
        End Function

        <Extension>
        Public Function IsGeneratedCode(tree As SyntaxTree, isComment As Func(Of SyntaxTrivia, Boolean), CancelToken As CancellationToken) As Boolean
            If isComment Is Nothing Then
                Throw New ArgumentNullException(NameOf(isComment))
            End If

            If tree Is Nothing Then
                Throw New ArgumentNullException(NameOf(tree))
            End If
            Return IsGeneratedCodeFile(tree.FilePath) OrElse BeginsWithAutoGeneratedComment(tree, isComment, CancelToken)
        End Function
    End Module
End Namespace
