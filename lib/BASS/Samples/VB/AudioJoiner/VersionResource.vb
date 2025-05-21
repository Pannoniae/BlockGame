'*** File Details *****************************************************************
'    File        : VersionResource.vb
'    Requires    : None
'    Type        : VB.NET Class
'    Description : Retrieves version resources from .exe and .dll
'--- Change Log   -----------------------------------------------------------------
'    06/16/2004 by ACTIVEASPMP\Rick - Initial Creation
'    12/27/2004 by ACTIVEASPMP\Rick - Option Strict compiles now
'**********************************************************************************

#Region " Library Imports "

Imports System.ComponentModel
Imports System.Reflection

#End Region

Public Class VersionResource

#Region " Private Variable Declarations "

#End Region

#Region " Private Property Declarations "

    Private _AssemblyName As String
    Private _Copyright As String
    Private _ProductName As String
    Private _ProductVersion As String
    Private _ProductCompany As String
    Private _Trademarks As String

#End Region

#Region " Private Methods "

    Private Sub pRefresh()

        ' Assembly Name property is required.
        If _AssemblyName = "" Then Exit Sub
        ' Check to see if the file exists
        If Not System.IO.File.Exists(_AssemblyName) Then Exit Sub

        Dim m_Assm As System.Reflection.Assembly = Nothing
        Dim objCopyright As AssemblyCopyrightAttribute = Nothing
        Dim objDescription As AssemblyDescriptionAttribute = Nothing
        Dim objCompany As AssemblyCompanyAttribute = Nothing
        Dim objTrademark As AssemblyTrademarkAttribute = Nothing
        Dim objProduct As AssemblyProductAttribute = Nothing
        Dim objTitle As AssemblyTitleAttribute = Nothing

        Dim VI As System.Diagnostics.FileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(_AssemblyName)
        _ProductVersion = VI.FileVersion.ToString

        Try
            m_Assm = System.Reflection.Assembly.LoadFile(_AssemblyName)
        Catch ex As Exception
        End Try
        Try
            objTitle = CType(AssemblyTitleAttribute.GetCustomAttribute(m_Assm, GetType(AssemblyTitleAttribute)), AssemblyTitleAttribute)
        Catch ex As Exception
        End Try
        Try
            objProduct = CType(AssemblyProductAttribute.GetCustomAttribute(m_Assm, GetType(AssemblyProductAttribute)), AssemblyProductAttribute)
        Catch ex As Exception
        End Try
        Try
            objDescription = CType(AssemblyDescriptionAttribute.GetCustomAttribute(m_Assm, GetType(AssemblyDescriptionAttribute)), AssemblyDescriptionAttribute)
        Catch ex As Exception
        End Try
        Try
            objCompany = CType(AssemblyCompanyAttribute.GetCustomAttribute(m_Assm, GetType(AssemblyCompanyAttribute)), AssemblyCompanyAttribute)
        Catch ex As Exception
        End Try
        Try
            objCopyright = CType(AssemblyCopyrightAttribute.GetCustomAttribute(m_Assm, GetType(AssemblyCopyrightAttribute)), AssemblyCopyrightAttribute)
        Catch ex As Exception
        End Try
        Try
            objTrademark = CType(AssemblyTrademarkAttribute.GetCustomAttribute(m_Assm, GetType(AssemblyTrademarkAttribute)), AssemblyTrademarkAttribute)
        Catch ex As Exception
        End Try
        Try
            _ProductName = objTitle.Title.ToString
        Catch ex As Exception
        End Try
        Try
            _Copyright = objCopyright.Copyright.ToString
        Catch ex As Exception
        End Try
        Try
            _ProductCompany = objCompany.Company.ToString
        Catch ex As Exception
        End Try
        Try
            _Trademarks = objTrademark.Trademark.ToString
        Catch ex As Exception

        End Try
    End Sub

#End Region

#Region " Public Property Declarations "

    '/ -----------------------------------------------------------------------------
    '/ <summary>
    '/ Returns/Sets AssemblyName Property
    '/ </summary>
    '/ <value></value>
    '/ <remarks>
    '/ </remarks>
    '/ <history>
    '/ 	[Rick]	12/28/2004	Created
    '/ </history>
    '/ -----------------------------------------------------------------------------
    <CategoryAttribute("Properties"), _
            Browsable(True), _
            [ReadOnly](False), _
            BindableAttribute(False), _
            DefaultValueAttribute(""), _
            DesignOnly(False), _
            DescriptionAttribute("Returns/Sets AssemblyName Property")> _
            Public Property AssemblyName() As String
        Get
            Return _AssemblyName
        End Get
        Set(ByVal Value As String)
            _AssemblyName = Value
        End Set
    End Property ' AssemblyName

    '/ -----------------------------------------------------------------------------
    '/ <summary>
    '/ Returns/Sets Copyright Property
    '/ </summary>
    '/ <value></value>
    '/ <remarks>
    '/ </remarks>
    '/ <history>
    '/ 	[Rick]	12/28/2004	Created
    '/ </history>
    '/ -----------------------------------------------------------------------------
    <CategoryAttribute("Properties"), _
            Browsable(True), _
            [ReadOnly](False), _
            BindableAttribute(False), _
            DefaultValueAttribute(""), _
            DesignOnly(False), _
            DescriptionAttribute("Returns/Sets Copyright Property")> _
            Public Property Copyright() As String
        Get
            Return _Copyright
        End Get
        Set(ByVal Value As String)
            _Copyright = Value
        End Set
    End Property ' Copyright

    '/ -----------------------------------------------------------------------------
    '/ <summary>
    '/ Returns/Sets ProductName Property
    '/ </summary>
    '/ <value></value>
    '/ <remarks>
    '/ </remarks>
    '/ <history>
    '/ 	[Rick]	12/28/2004	Created
    '/ </history>
    '/ -----------------------------------------------------------------------------
    <CategoryAttribute("Properties"), _
            Browsable(True), _
            [ReadOnly](False), _
            BindableAttribute(False), _
            DefaultValueAttribute(""), _
            DesignOnly(False), _
            DescriptionAttribute("Returns/Sets ProductName Property")> _
            Public Property ProductName() As String
        Get
            Return _ProductName
        End Get
        Set(ByVal Value As String)
            _ProductName = Value
        End Set
    End Property ' ProductName

    '/ -----------------------------------------------------------------------------
    '/ <summary>
    '/ Returns/Sets ProductVersion Property
    '/ </summary>
    '/ <value></value>
    '/ <remarks>
    '/ </remarks>
    '/ <history>
    '/ 	[Rick]	12/28/2004	Created
    '/ </history>
    '/ -----------------------------------------------------------------------------
    <CategoryAttribute("Properties"), _
            Browsable(True), _
            [ReadOnly](False), _
            BindableAttribute(False), _
            DefaultValueAttribute(""), _
            DesignOnly(False), _
            DescriptionAttribute("Returns/Sets ProductVersion Property")> _
            Public Property ProductVersion() As String
        Get
            Return _ProductVersion
        End Get
        Set(ByVal Value As String)
            _ProductVersion = Value
        End Set
    End Property ' ProductVersion

    '/ -----------------------------------------------------------------------------
    '/ <summary>
    '/ Returns/Sets Company Property
    '/ </summary>
    '/ <value></value>
    '/ <remarks>
    '/ </remarks>
    '/ <history>
    '/ 	[Rick]	12/28/2004	Created
    '/ </history>
    '/ -----------------------------------------------------------------------------
    <CategoryAttribute("Properties"), _
            Browsable(True), _
            [ReadOnly](False), _
            BindableAttribute(False), _
            DefaultValueAttribute(""), _
            DesignOnly(False), _
            DescriptionAttribute("Returns/Sets Company Property")> _
            Public Property Company() As String
        Get
            Return _ProductCompany
        End Get
        Set(ByVal Value As String)
            _ProductCompany = Value
        End Set
    End Property ' Company

    '/ -----------------------------------------------------------------------------
    '/ <summary>
    '/ Returns/Sets Trademarks Property
    '/ </summary>
    '/ <value></value>
    '/ <remarks>
    '/ </remarks>
    '/ <history>
    '/ 	[Rick]	12/28/2004	Created
    '/ </history>
    '/ -----------------------------------------------------------------------------
    <CategoryAttribute("Properties"), _
            Browsable(True), _
            [ReadOnly](False), _
            BindableAttribute(False), _
            DefaultValueAttribute(""), _
            DesignOnly(False), _
            DescriptionAttribute("Returns/Sets Trademark Property")> _
            Public Property Trademark() As String
        Get
            Return _Trademarks
        End Get
        Set(ByVal Value As String)
            _Trademarks = Value
        End Set
    End Property ' Trademark

#End Region

#Region " Constructor / Deconstructor "

    Public Sub New()
        ' Enter Class Creation Code Here

    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()

    End Sub

#End Region

#Region " Public Methods "

    '/ -----------------------------------------------------------------------------
    '/ <summary>
    '/ Loads the version information from the assembly
    '/ </summary>
    '/ <remarks>Uses System.Reflection
    '/ </remarks>
    '/ <history>
    '/ 	[Rick]	12/28/2004	Created
    '/ </history>
    '/ -----------------------------------------------------------------------------
    Public Sub Refresh()
        pRefresh()
    End Sub

#End Region

End Class
