﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports ProgressReportLibrary

Namespace CSharpToVBConverter

    Public Class ConvertRequest

        Public Sub New(mSkipAutoGenerated As Boolean, mProgress As IProgress(Of ProgressReport), mCancelToken As CancellationToken)
            _Progress = mProgress
            _SkipAutoGenerated = mSkipAutoGenerated
            _CancelToken = mCancelToken
        End Sub

        Public Property CancelToken As CancellationToken
        Public ReadOnly Property Progress As IProgress(Of ProgressReport)
        Public ReadOnly Property SkipAutoGenerated As Boolean
        Public Property SourceCode As String

    End Class

End Namespace
