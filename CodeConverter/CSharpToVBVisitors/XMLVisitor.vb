﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics.CodeAnalysis

Imports Microsoft.CodeAnalysis

Imports CS = Microsoft.CodeAnalysis.CSharp
Imports CSS = Microsoft.CodeAnalysis.CSharp.Syntax
Imports Factory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory
Imports VB = Microsoft.CodeAnalysis.VisualBasic
Imports VBS = Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace CSharpToVBConverter.ToVisualBasic

    Friend Class XMLVisitor
        Inherits CS.CSharpSyntaxVisitor(Of VB.VisualBasicSyntaxNode)

        Private Shared Function GetVBOperatorToken(op As String) As SyntaxToken
            Select Case op
                Case "=="
                    Return EqualsToken
                Case "!="
                    Return LessThanGreaterThanToken
                Case ">"
                    Return GreaterThanToken
                Case ">="
                    Return GreaterThanEqualsToken
                Case "<"
                    Return LessThanToken
                Case "<="
                    Return LessThanEqualsToken
                Case "|"
                    Return OrKeyword
                Case "||"
                    Return OrElseKeyword
                Case "&"
                    Return AndKeyword
                Case "&&"
                    Return AndAlsoKeyword
                Case "+"
                    Return PlusToken
                Case "-"
                    Return MinusToken
                Case "*"
                    Return AsteriskToken
                Case "/"
                    Return SlashToken
                Case "%"
                    Return ModKeyword
                Case "="
                    Return EqualsToken
                Case "+="
                    Return PlusEqualsToken
                Case "-="
                    Return MinusEqualsToken
                Case "!"
                    Return NotKeyword
                Case "~"
                    Return NotKeyword
                Case Else

            End Select

            Throw New ArgumentOutOfRangeException(NameOf(op))
        End Function

        Private Function GatherAttributes(listOfAttributes As SyntaxList(Of CSS.XmlAttributeSyntax)) As SyntaxList(Of VBS.XmlNodeSyntax)
            Dim VBAttributes As New SyntaxList(Of VBS.XmlNodeSyntax)
            For Each a As CSS.XmlAttributeSyntax In listOfAttributes
                VBAttributes = VBAttributes.Add(DirectCast(a.Accept(Me).WithConvertedLeadingTriviaFrom(a), VBS.XmlNodeSyntax))
            Next
            Return VBAttributes
        End Function

        <ExcludeFromCodeCoverage>
        Public Overrides Function DefaultVisit(node As SyntaxNode) As VB.VisualBasicSyntaxNode
            Return MyBase.DefaultVisit(node)
        End Function

        Public Overrides Function VisitConversionOperatorMemberCref(node As CSS.ConversionOperatorMemberCrefSyntax) As VB.VisualBasicSyntaxNode
            Return MyBase.VisitConversionOperatorMemberCref(node)
        End Function

        Public Overrides Function VisitCrefBracketedParameterList(node As CSS.CrefBracketedParameterListSyntax) As VB.VisualBasicSyntaxNode
            Return MyBase.VisitCrefBracketedParameterList(node)
        End Function

        Public Overrides Function VisitCrefParameter(node As CSS.CrefParameterSyntax) As VB.VisualBasicSyntaxNode
            Return node.Type.Accept(Me)
        End Function

        Public Overrides Function VisitCrefParameterList(node As CSS.CrefParameterListSyntax) As VB.VisualBasicSyntaxNode
            Return MyBase.VisitCrefParameterList(node)
        End Function

        Public Overrides Function VisitGenericName(node As CSS.GenericNameSyntax) As VB.VisualBasicSyntaxNode
            Dim identifier As SyntaxToken = Factory.Identifier(node.Identifier.ToString)
            Dim TypeList As New List(Of VBS.TypeSyntax)
            For Each a As CSS.TypeSyntax In node.TypeArgumentList.Arguments
                Dim TypeIdentifier As VBS.TypeSyntax = DirectCast(a.Accept(Me), VBS.TypeSyntax)
                TypeList.Add(TypeIdentifier)
            Next
            Return Factory.GenericName(identifier, FactoryTypeArgumentList(TypeList))
        End Function

        Public Overrides Function VisitIdentifierName(node As CSS.IdentifierNameSyntax) As VB.VisualBasicSyntaxNode
            Dim identifier As VBS.IdentifierNameSyntax = Factory.IdentifierName(node.Identifier.ToString)
            Return identifier
        End Function

        Public Overrides Function VisitNameMemberCref(node As CSS.NameMemberCrefSyntax) As VB.VisualBasicSyntaxNode
            Dim Name As VB.VisualBasicSyntaxNode = node.Name.Accept(Me)
            Dim CrefParameters As New List(Of VBS.CrefSignaturePartSyntax)
            Dim Signature As VBS.CrefSignatureSyntax = Nothing
            If node.Parameters IsNot Nothing Then
                For Each p As CSS.CrefParameterSyntax In node.Parameters.Parameters
                    Dim TypeSyntax1 As VBS.TypeSyntax = DirectCast(p.Accept(Me), VBS.TypeSyntax)
                    CrefParameters.Add(Factory.CrefSignaturePart(modifier:=Nothing, TypeSyntax1))
                Next
                Signature = Factory.CrefSignature(CrefParameters.ToArray)
            End If
            Return Factory.CrefReference(DirectCast(Name, VBS.TypeSyntax), signature:=Signature, asClause:=Nothing)
        End Function

        Public Overrides Function VisitOperatorMemberCref(node As CSS.OperatorMemberCrefSyntax) As VB.VisualBasicSyntaxNode
            Dim CrefOperator As SyntaxToken = GetVBOperatorToken(node.OperatorToken.ValueText)
            Return Factory.CrefOperatorReference(CrefOperator.WithLeadingTrivia(Factory.Space))
        End Function

        Public Overrides Function VisitPredefinedType(node As CSS.PredefinedTypeSyntax) As VB.VisualBasicSyntaxNode
            Dim Token As SyntaxToken = CS.CSharpExtensions.Kind(node.Keyword).GetTypeToken(context:=TokenContext.XMLComment)
            Select Case Token.RawKind
                Case VB.SyntaxKind.EmptyToken
                    Return Factory.ParseTypeName(node.ToString)
                Case VB.SyntaxKind.NothingKeyword
                    Return NothingExpression
                Case Else
                    Return Factory.PredefinedType(Token)
            End Select
        End Function

        Public Overrides Function VisitQualifiedCref(QualifiedCref As CSS.QualifiedCrefSyntax) As VB.VisualBasicSyntaxNode
            Dim IdentifierOrTypeName As VB.VisualBasicSyntaxNode = QualifiedCref.Container.Accept(Me)
            Dim Value As VBS.CrefReferenceSyntax = DirectCast(QualifiedCref.Member.Accept(Me), VBS.CrefReferenceSyntax)
            Dim identifier As VBS.NameSyntax
            identifier = If(TypeOf IdentifierOrTypeName Is VBS.NameSyntax, DirectCast(IdentifierOrTypeName, VBS.NameSyntax), Factory.IdentifierName(IdentifierOrTypeName.ToString))
            Dim QualifiedNameSyntax As VBS.QualifiedNameSyntax = Factory.QualifiedName(left:=identifier,
                                                                                                DotToken,
                                                                                                right:=DirectCast(Value.Name, VBS.SimpleNameSyntax))
            If Value.Signature Is Nothing Then
                Return QualifiedNameSyntax
            End If
            Return Factory.CrefReference(QualifiedNameSyntax, Value.Signature, Nothing)
        End Function

        Public Overrides Function VisitQualifiedName(node As CSS.QualifiedNameSyntax) As VB.VisualBasicSyntaxNode
            Return Factory.QualifiedName(DirectCast(node.Left.Accept(Me), VBS.NameSyntax), DirectCast(node.Right.Accept(Me), VBS.SimpleNameSyntax))
        End Function

        Public Overrides Function VisitTypeCref(node As CSS.TypeCrefSyntax) As VB.VisualBasicSyntaxNode
            Return node.Type.Accept(Me)
        End Function

        Public Overrides Function VisitXmlCDataSection(node As CSS.XmlCDataSectionSyntax) As VB.VisualBasicSyntaxNode
            Dim TextTokens As SyntaxTokenList = TranslateTokenList(node.TextTokens)
            Return Factory.XmlCDataSection(BeginCDataToken, TextTokens, EndCDataToken)
        End Function

        Public Overrides Function VisitXmlComment(node As CSS.XmlCommentSyntax) As VB.VisualBasicSyntaxNode
            Return MyBase.VisitXmlComment(node).WithConvertedTriviaFrom(node)
        End Function

        Public Overrides Function VisitXmlCrefAttribute(node As CSS.XmlCrefAttributeSyntax) As VB.VisualBasicSyntaxNode
            Dim Name As VBS.XmlNameSyntax = DirectCast(node.Name.Accept(Me), VBS.XmlNameSyntax)

            Dim cref As VB.VisualBasicSyntaxNode = node.Cref.Accept(Me)
            Dim SyntaxTokens As New SyntaxTokenList
            SyntaxTokens = SyntaxTokens.AddRange(cref.DescendantTokens)
            Dim Value As VBS.XmlNodeSyntax = Factory.XmlString(
                            DoubleQuoteToken,
                            SyntaxTokens,
                            DoubleQuoteToken).WithConvertedTriviaFrom(node)
            Return Factory.XmlAttribute(Name, Value)
        End Function

        Public Overrides Function VisitXmlElement(node As CSS.XmlElementSyntax) As VB.VisualBasicSyntaxNode
            Dim Content As New SyntaxList(Of VBS.XmlNodeSyntax)
            Dim StartTag As VBS.XmlElementStartTagSyntax = DirectCast(node.StartTag.Accept(Me).WithConvertedTriviaFrom(node.StartTag), VBS.XmlElementStartTagSyntax)

            Dim NoEndTag As Boolean = String.IsNullOrWhiteSpace(node.EndTag.Name.LocalName.ValueText)
            Dim EndTag As VBS.XmlElementEndTagSyntax = If(NoEndTag,
                Factory.XmlElementEndTag(DirectCast(StartTag.Name, VBS.XmlNameSyntax).WithConvertedTriviaFrom(node.EndTag)),
                Factory.XmlElementEndTag(DirectCast(node.EndTag.Name.Accept(Me), VBS.XmlNameSyntax)))
            Try
                For Each e As IndexClass(Of CSS.XmlNodeSyntax) In node.Content.WithIndex
                    Dim vbNode As VBS.XmlNodeSyntax = CType(e.Value.Accept(Me).WithConvertedTriviaFrom(e.Value), VBS.XmlNodeSyntax)
                    If NoEndTag Then
                        Dim LastToken As SyntaxToken = vbNode.GetLastToken
                        If LastToken.ValueText.IsNewLine Then
                            vbNode = vbNode.ReplaceToken(LastToken, Factory.Token(VB.SyntaxKind.EmptyToken))
                        End If
                    End If
                    Content = Content.Add(vbNode)
                Next

                If node.EndTag?.HasLeadingTrivia AndAlso node.EndTag.GetLeadingTrivia(0).IsKind(CS.SyntaxKind.DocumentationCommentExteriorTrivia) Then
                    Dim NewLeadingTriviaList As New SyntaxTriviaList
                    NewLeadingTriviaList = NewLeadingTriviaList.Add(Factory.DocumentationCommentExteriorTrivia(node.EndTag.GetLeadingTrivia(0).ToString.Replace("///", "'''", StringComparison.Ordinal)))
                    Dim NewTokenList As New SyntaxTokenList
                    NewTokenList = NewTokenList.Add(Factory.XmlTextLiteralToken(NewLeadingTriviaList, " ", " ", New SyntaxTriviaList))
                    Content = Content.Add(Factory.XmlText(NewTokenList))
                    EndTag = EndTag.WithoutLeadingTrivia
                End If
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                Stop
            End Try
            Dim XmlElement As VBS.XmlElementSyntax = Factory.XmlElement(StartTag, Content, EndTag)
            Return XmlElement
        End Function

        Public Overrides Function VisitXmlElementEndTag(node As CSS.XmlElementEndTagSyntax) As VB.VisualBasicSyntaxNode
            Return Factory.XmlElementEndTag(DirectCast(node.Name.Accept(Me), VBS.XmlNameSyntax))
        End Function

        Public Overrides Function VisitXmlElementStartTag(node As CSS.XmlElementStartTagSyntax) As VB.VisualBasicSyntaxNode
            Dim listOfAttributes As SyntaxList(Of VBS.XmlNodeSyntax) = Me.GatherAttributes(node.Attributes)
            Return Factory.XmlElementStartTag(DirectCast(node.Name.Accept(Me), VBS.XmlNodeSyntax), listOfAttributes)
        End Function

        Public Overrides Function VisitXmlEmptyElement(node As CSS.XmlEmptyElementSyntax) As VB.VisualBasicSyntaxNode
            Try
                Dim Name As VBS.XmlNodeSyntax = DirectCast(node.Name.Accept(Me), VBS.XmlNodeSyntax)
                Dim listOfAttributes As SyntaxList(Of VBS.XmlNodeSyntax) = Me.GatherAttributes(node.Attributes)
                Return Factory.XmlEmptyElement(Name, listOfAttributes).WithConvertedTriviaFrom(node)
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                Return Factory.XmlText(node.GetText.ToString)
            End Try
        End Function

        Public Overrides Function VisitXmlName(node As CSS.XmlNameSyntax) As VB.VisualBasicSyntaxNode
            Dim Prefix As VBS.XmlPrefixSyntax
            Prefix = If(node.Prefix Is Nothing, Nothing, DirectCast(node.Prefix.Accept(Me), VBS.XmlPrefixSyntax))
            Dim localName As SyntaxToken = Factory.XmlNameToken(node.LocalName.ValueText, Nothing)
            Return Factory.XmlName(Prefix, localName)
        End Function

        Public Overrides Function VisitXmlNameAttribute(node As CSS.XmlNameAttributeSyntax) As VB.VisualBasicSyntaxNode
            Dim Name As VBS.XmlNodeSyntax = DirectCast(node.Name.Accept(Me), VBS.XmlNodeSyntax).WithConvertedLeadingTriviaFrom(node.Name)
            Dim ValueString As String = node.Identifier.ToString
            Dim Value As VBS.XmlNodeSyntax = Factory.XmlString(
                                                    DoubleQuoteToken,
                                                    SyntaxTokenList.Create(
                                                    Factory.XmlTextLiteralToken(ValueString, ValueString)),
                                                    DoubleQuoteToken)
            Return Factory.XmlAttribute(Name, Value).WithConvertedTriviaFrom(node).WithConvertedTriviaFrom(node)
        End Function

        Public Overrides Function VisitXmlPrefix(node As CSS.XmlPrefixSyntax) As VB.VisualBasicSyntaxNode
            Return MyBase.VisitXmlPrefix(node).WithConvertedTriviaFrom(node)
        End Function

        Public Overrides Function VisitXmlProcessingInstruction(node As CSS.XmlProcessingInstructionSyntax) As VB.VisualBasicSyntaxNode
            Return MyBase.VisitXmlProcessingInstruction(node).WithConvertedTriviaFrom(node)
        End Function

        Public Overrides Function VisitXmlText(node As CSS.XmlTextSyntax) As VB.VisualBasicSyntaxNode
            Dim TextTokens As SyntaxTokenList = TranslateTokenList(node.TextTokens)
            Dim XmlText As VBS.XmlTextSyntax = Factory.XmlText(TextTokens)
            Return XmlText
        End Function

        Public Overrides Function VisitXmlTextAttribute(node As CSS.XmlTextAttributeSyntax) As VB.VisualBasicSyntaxNode
            Dim Name As VBS.XmlNodeSyntax = DirectCast(node.Name.Accept(Me).WithConvertedTriviaFrom(node.Name), VBS.XmlNodeSyntax)
            Dim TextTokens As SyntaxTokenList = TranslateTokenList(node.TextTokens)
            Dim XmlText As VBS.XmlTextSyntax = Factory.XmlText(TextTokens)
            Dim Value As VBS.XmlNodeSyntax = Factory.XmlString(
                                                DoubleQuoteToken,
                                                SyntaxTokenList.Create(
                                                Factory.XmlTextLiteralToken(XmlText.ToString, XmlText.ToString)),
                                                DoubleQuoteToken)
            Return Factory.XmlAttribute(Name, Value)
        End Function

    End Class

End Namespace
