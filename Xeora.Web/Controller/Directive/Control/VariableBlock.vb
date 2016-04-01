﻿Option Strict On
Imports Xeora.Web.Shared

Namespace Xeora.Web.Controller.Directive.Control
    Public Class VariableBlock
        Inherits ControlBase
        Implements IInstanceRequires

        Public Event InstanceRequested(ByRef Instance As IDomain) Implements IInstanceRequires.InstanceRequested

        Public Sub New(ByVal DraftStartIndex As Integer, ByVal DraftValue As String, ByVal ContentArguments As [Global].ArgumentInfo.ArgumentInfoCollection)
            MyBase.New(DraftStartIndex, DraftValue, ControlTypes.VariableBlock, ContentArguments)
        End Sub

        Public Overrides Sub Render(ByRef SenderController As ControllerBase)
            If Me.IsUpdateBlockRequest AndAlso Not Me.InRequestedUpdateBlock Then
                Me.DefineRenderedValue(String.Empty)

                Exit Sub
            End If

            If Not String.IsNullOrEmpty(Me.BoundControlID) Then
                If Me.IsRendered Then Exit Sub

                If Not Me.BoundControlRenderWaiting Then
                    Dim Controller As ControllerBase = Me

                    Do Until Controller.Parent Is Nothing
                        If TypeOf Controller.Parent Is ControllerBase AndAlso
                            TypeOf Controller.Parent Is INamable Then

                            If String.Compare(
                                CType(Controller.Parent, INamable).ControlID, Me.BoundControlID, True) = 0 Then

                                Throw New Exception.InternalParentException(Exception.InternalParentException.ChildDirectiveTypes.Control)
                            End If
                        End If

                        Controller = Controller.Parent
                    Loop

                    Me.RegisterToRenderCompleted()
                End If

                If TypeOf SenderController Is ControlBase AndAlso
                    TypeOf SenderController Is INamable Then

                    If String.Compare(
                        CType(SenderController, INamable).ControlID, Me.BoundControlID, True) <> 0 Then

                        Exit Sub
                    Else
                        Me.RenderInternal()
                    End If
                End If
            Else
                Me.RenderInternal()
            End If
        End Sub

        Private Sub RenderInternal()
            ' Parse Block Content
            Dim controlValueSplitted As String() =
                Me.InsideValue.Split(":"c)
            Dim BlockContent As String = String.Join(":", controlValueSplitted, 1, controlValueSplitted.Length - 1)

            ' Check This Control has a Content
            Dim idxCon As Integer = BlockContent.IndexOf(":"c)

            ' Get ControlID Accourding to idxCon Value -1 = no content, else has content
            If idxCon = -1 Then
                ' No Content

                Throw New Exception.GrammerException()
            End If

            ' ControlIDWithIndex Like ControlID~INDEX
            Dim ControlIDWithIndex As String = BlockContent.Substring(0, idxCon)

            Dim CoreContent As String = Nothing
            Dim idxCoreContStart As Integer, idxCoreContEnd As Integer

            Dim OpeningTag As String = String.Format("{0}:{{", ControlIDWithIndex)
            Dim ClosingTag As String = String.Format("}}:{0}", ControlIDWithIndex)

            idxCoreContStart = BlockContent.IndexOf(OpeningTag) + OpeningTag.Length
            idxCoreContEnd = BlockContent.LastIndexOf(ClosingTag, BlockContent.Length)

            If idxCoreContStart = OpeningTag.Length AndAlso
                idxCoreContEnd = (BlockContent.Length - OpeningTag.Length) Then

                CoreContent = BlockContent.Substring(idxCoreContStart, idxCoreContEnd - idxCoreContStart)

                If Not CoreContent Is Nothing AndAlso
                    CoreContent.Trim().Length > 0 Then

                    ' Call Related Function and Exam It
                    Dim ControllerLevel As ControllerBase = Me
                    Dim Leveling As Integer = Me.Level

                    Do
                        If Leveling > 0 Then
                            ControllerLevel = ControllerLevel.Parent

                            If TypeOf ControllerLevel Is RenderlessController Then _
                                ControllerLevel = ControllerLevel.Parent

                            Leveling -= 1
                        End If
                    Loop Until ControllerLevel Is Nothing OrElse Leveling = 0

                    Dim BindInvokeResult As [Shared].Execution.BindInvokeResult =
                        Manager.Assembly.InvokeBind(
                            Me.BindInfo,
                            PropertyController.ParseProperties(Me, ControllerLevel.ContentArguments, Me.BindInfo.ProcedureParams, New IInstanceRequires.InstanceRequestedEventHandler(Sub(ByRef Instance As IDomain)
                                                                                                                                                                                          RaiseEvent InstanceRequested(Instance)
                                                                                                                                                                                      End Sub)),
                            Manager.Assembly.ExecuterTypes.Control
                        )

                    Dim VariableBlockResult As ControlResult.VariableBlock = Nothing

                    If BindInvokeResult.ReloadRequired Then
                        Throw New Exception.ReloadRequiredException(BindInvokeResult.ApplicationPath)
                    Else
                        If Not BindInvokeResult.InvokeResult Is Nothing AndAlso
                            TypeOf BindInvokeResult.InvokeResult Is System.Exception Then

                            Throw New Exception.ExecutionException(
                                CType(BindInvokeResult.InvokeResult, System.Exception).Message,
                                CType(BindInvokeResult.InvokeResult, System.Exception).InnerException
                            )
                        Else
                            VariableBlockResult = CType(BindInvokeResult.InvokeResult, ControlResult.VariableBlock)
                        End If
                    End If
                    ' ----

                    ' if VariableBlockResult is not nothing, Set Variable Values
                    If Not VariableBlockResult Is Nothing Then
                        For Each Key As String In VariableBlockResult.Keys
                            ControllerLevel.ContentArguments.Add(Key, VariableBlockResult.Item(Key))
                        Next
                    End If

                    Me.RequestParse(CoreContent, ControllerLevel)

                    Me.DefineRenderedValue(Me.Create())
                    ' ----
                Else
                    Throw New Exception.EmptyBlockException()
                End If
            Else
                Throw New Exception.ParseException()
            End If
        End Sub
    End Class
End Namespace