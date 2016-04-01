﻿Namespace Xeora.VSAddIn
    Public Class AddInLoaderHelper
        Private Shared _AppDomain As AppDomain = Nothing
        Private Shared _AddInLoader As Xeora.VSAddIn.IAddInLoader = Nothing

        Public Shared Sub CreateAppDomain()
            If AddInLoaderHelper._AppDomain Is Nothing Then
                Dim ExecutingAssembly As System.Reflection.Assembly =
                    System.Reflection.Assembly.GetExecutingAssembly()
                Dim ExecutingPath As String =
                    IO.Path.GetDirectoryName(ExecutingAssembly.Location)

                AddHandler System.AppDomain.CurrentDomain.AssemblyResolve, AddressOf AddInLoaderHelper.AssemblyResolve

                Dim AppDomainInfo As New AppDomainSetup()

                With AppDomainInfo
                    .ApplicationName = "XeoraAddInLoaderDomain"
                    .ApplicationBase = ExecutingPath
                    .PrivateBinPath = String.Format("{0};{1}", ExecutingPath, System.AppDomain.CurrentDomain.RelativeSearchPath)
                    .ShadowCopyFiles = Boolean.TrueString
                End With

                AddInLoaderHelper._AppDomain =
                    System.AppDomain.CreateDomain(
                        "XeoraAddInLoaderDomain",
                        System.AppDomain.CurrentDomain.Evidence,
                        AppDomainInfo
                    )
                AddHandler AddInLoaderHelper._AppDomain.AssemblyResolve, AddressOf AddInLoaderHelper.AssemblyResolve
            End If
        End Sub

        Private Shared Function AssemblyResolve(ByVal sender As Object, ByVal e As System.ResolveEventArgs) As System.Reflection.Assembly
            Dim rAssembly As System.Reflection.Assembly = Nothing

            For Each assembly As System.Reflection.Assembly In System.AppDomain.CurrentDomain.GetAssemblies()
                If assembly.FullName = e.Name Then _
                    rAssembly = assembly : Exit For
            Next

            Return rAssembly
        End Function

        Public Shared Sub DestroyAppDomain()
            If Not AddInLoaderHelper._AppDomain Is Nothing Then
                RemoveHandler System.AppDomain.CurrentDomain.AssemblyResolve, AddressOf AddInLoaderHelper.AssemblyResolve

                System.AppDomain.Unload(AddInLoaderHelper._AppDomain)

                AddInLoaderHelper._AppDomain = Nothing

                Dim TempLocation As String =
                    IO.Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "XeoraCubeAddInTemp")

                Try
                    If IO.Directory.Exists(TempLocation) Then IO.Directory.Delete(TempLocation, True)
                Catch ex As Exception
                    ' Just Handle Exceptions
                End Try
            End If
        End Sub

        Public Shared ReadOnly Property AddInLoader As Xeora.VSAddIn.IAddInLoader
            Get
                If Not AddInLoaderHelper._AppDomain Is Nothing AndAlso
                    AddInLoaderHelper._AddInLoader Is Nothing Then

                    AddInLoaderHelper._AddInLoader =
                        CType(
                            AddInLoaderHelper._AppDomain.CreateInstanceAndUnwrap(
                                "Xeora.AddInLoader",
                                "Xeora.VSAddIn.AddInLoader"),
                            Xeora.VSAddIn.IAddInLoader
                        )
                End If

                Return AddInLoaderHelper._AddInLoader
            End Get
        End Property
    End Class
End Namespace