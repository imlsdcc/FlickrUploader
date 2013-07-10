Imports System.Configuration
Imports FlickrNet
Imports System.IO
Imports System.Xml
Imports System.Text


Module Module1
    Private frob As String

    Sub Main()
        Dim f As FlickrNet.Flickr = New FlickrNet.Flickr(ConfigurationManager.AppSettings.Item("flickrKey"), ConfigurationManager.AppSettings.Item("flickrSecret"))

        frob = f.AuthGetFrob()

        Console.Out.WriteLine("Frob = " & frob)

        Dim url As String = f.AuthCalcUrl(frob, AuthLevel.Write)

        Console.Out.WriteLine("Url = " & url)

        System.Diagnostics.Process.Start(url)
        Console.Out.WriteLine("Authorization is pending. Press [Enter] when it is completed.")
        Console.In.ReadLine()

        Dim AuthToken As String = ""

        Try

            Dim a As Auth = f.AuthGetToken(frob)
            Console.Out.WriteLine("User Authenticated = " & a.User.UserName)
            Console.Out.WriteLine("Auth Token = " & a.Token)
            AuthToken = a.Token

        Catch ex As Exception

            Console.Out.WriteLine("Authentication failed: " & ex.Message)

        End Try

        'Filename.Text = OpenFileDialog1.FileName
        If AuthToken = "" Then
            'do nothing
        Else
            Dim baseurl As String = ConfigurationManager.AppSettings.Item("baseurl")
            Dim basetext As String = ConfigurationManager.AppSettings.Item("textBlock")
            Dim f2 As FlickrNet.Flickr = New FlickrNet.Flickr(ConfigurationManager.AppSettings.Item("flickrKey"), ConfigurationManager.AppSettings.Item("flickrSecret"), AuthToken)
            Dim fls = Directory.EnumerateFiles(ConfigurationManager.AppSettings.Item("ImageDir"))
            Dim odatdir As String = ConfigurationManager.AppSettings.Item("DatDir")
            Dim cnt As Integer = 0

            For Each fl In fls
                Console.Out.WriteLine(fl)

                Dim dbo = New Flickr_UpDataContext
                'cnt = cnt + 1
                Dim uploadAsPublic As Boolean = False
                'Dim title As String = "Test title " & cnt
                'Dim description As String = "Test Description " & cnt
                Console.Out.WriteLine(Path.GetExtension(fl))

                If Path.GetExtension(fl) = ".jpg" Or Path.GetExtension(fl) = ".JPG" Or Path.GetExtension(fl) = ".jpeg" Or Path.GetExtension(fl) = ".JPEG" Then

                    'Dim nodes
                    Dim identifier As String = ""
                    Dim datestamp As String = ""
                    Dim title As String = ""
                    Dim creator As String = ""
                    Dim creatorpt As String = ""
                    Dim predescription As String = ""
                    Dim description As String = ""
                    Dim jpgname As String = Path.GetFileNameWithoutExtension(fl)
                    Dim filepath As String = odatdir & "\" & jpgname & ".xml"
                    'Console.Out.WriteLine(filepath)
                    Dim xdoc As New XmlDocument
                    Dim xmlns As New XmlNamespaceManager(xdoc.NameTable)

                    xdoc.Load(filepath)
                    xmlns.AddNamespace("oai", "http://www.openarchives.org/OAI/2.0/")
                    xmlns.AddNamespace("oai_dc", "http://www.openarchives.org/OAI/2.0/oai_dc/")
                    xmlns.AddNamespace("dc", "http://purl.org/dc/elements/1.1/")

                    Dim node1 As XmlNode = xdoc.SelectSingleNode("//oai:identifier", xmlns)

                    If node1 IsNot Nothing Then
                        identifier = node1.InnerText
                    End If
                    'Console.Out.WriteLine("OAI Identifier: " & identifier)

                    Dim fRow = From f1 In dbo.Flickrs Where f1.identifier = identifier

                    If fRow.Count > 0 Then Continue For

                    Dim node8 As XmlNode = xdoc.SelectSingleNode("//oai:datestamp", xmlns)
                    If node8 IsNot Nothing Then
                        datestamp = node8.InnerText
                    End If

                    Dim node2 As XmlNode = xdoc.SelectSingleNode("//dc:title", xmlns)
                    If node2 IsNot Nothing Then
                        title = node2.InnerText
                    End If
                    'Console.Out.WriteLine("Title: " & title)

                    cnt = 0
                    Dim CreaNodes As XmlNodeList = xdoc.SelectNodes("//dc:creator", xmlns)
                    For Each node3 As XmlNode In CreaNodes
                        cnt = cnt + 1
                        If node3 IsNot Nothing Then
                            creator = node3.InnerText
                            Dim lcreator As String = LCase(creator)
                            If lcreator <> "unknown" Then
                                If cnt > 1 Then
                                    creatorpt = creatorpt & ",&nbsp;" & creator
                                Else
                                    creatorpt = creator
                                End If
                            End If
                        End If
                    Next
                    'Console.Out.WriteLine("Creators: " & creatorpt)

                    cnt = 0
                    Dim DescNodes As XmlNodeList = xdoc.SelectNodes("//dc:description", xmlns)
                    For Each node4 As XmlNode In DescNodes
                        cnt = cnt + 1
                        If node4 IsNot Nothing Then
                            If cnt > 1 Then
                                predescription = predescription & "&nbsp;" & node4.InnerText
                            Else
                                predescription = node4.InnerText
                            End If
                        End If
                    Next
                    'Console.Out.WriteLine("Description: " & predescription)

                    Dim IdentNodes As XmlNodeList = xdoc.SelectNodes("//dc:identifier", xmlns)
                    For Each node5 As XmlNode In IdentNodes
                        If node5 IsNot Nothing Then
                            If InStr(node5.InnerText, baseurl) <> 0 Then
                                url = node5.InnerText
                            End If
                        End If
                    Next
                    'Console.Out.WriteLine("URL: " & url)

                    'build description text block
                    If creatorpt <> "" Then
                        If cnt > 1 Then
                            creator = "<b>Creators:</b>&nbsp;" & creatorpt & vbCrLf & vbCrLf
                        Else
                            creator = "<b>Creator:</b>&nbsp;" & creatorpt & vbCrLf & vbCrLf
                        End If
                        description = creator
                    Else
                        'do nothing (no creator case)
                    End If

                    If predescription <> "" Then
                        If InStr(predescription, "<br>") <> 0 Then
                            predescription = Replace(predescription, "<br>", "&nbsp;")
                        End If
                        If InStr(predescription, "&lt;br&gt;") <> 0 Then
                            predescription = Replace(predescription, "&lt;br&gt;", "&nbsp;")
                        End If
                        description = description & "<b>Description:</b>&nbsp;" & predescription & vbCrLf & vbCrLf
                    Else
                        'do nothing
                    End If

                    description = description & "<a href='" & url & "' rel='nofollow'>View source image</a>."

                    Using sr As New StreamReader(basetext)
                        Dim line As String
                        Do
                            line = sr.ReadLine()
                            If Not (line Is Nothing) Then
                                description = description & vbCrLf & vbCrLf & line
                            End If
                        Loop Until line Is Nothing
                    End Using

                    'Now need to go back and parse/set the coverage tags
                    Dim geoString As String = ""
                    Dim geoTag As String = ""
                    cnt = 0
                    Dim GeoNodes As XmlNodeList = xdoc.SelectNodes("//dc:coverage", xmlns)
                    For Each node6 As XmlNode In GeoNodes
                        If node6 IsNot Nothing Then
                            geoString = node6.InnerText

                            If InStr(geoString, "&lt;br&gt;") <> 0 Then
                                geoString = Replace(geoString, "&lt;br&gt;", "--")
                            End If

                            If InStr(geoString, "<br>") <> 0 Then
                                geoString = Replace(geoString, "<br>", "--")
                            End If

                            Dim geoArray As Array = Split(geoString, "--")

                            For cnt = 0 To UBound(geoArray)
                                geoTag = geoTag & " " & """" & geoArray(cnt) & """"
                            Next
                        End If
                    Next
                    Console.Out.WriteLine("GeoTag: " & geoTag)

                    Dim tags As String = ""

                    If geoTag <> "" Then
                        tags = geoTag & " " & """IMLS DCC"""
                    Else
                        tags = """IMLS DCC"""
                    End If

                    Dim photoId As String = f2.UploadPicture(fl, title, description, tags, uploadAsPublic, False, False) 'note these last 2 (is Family, is Friend) = always false

                    Dim dateT
                    Dim pdate As String = ""
                    Dim tempDate As String = ""
                    Dim datePost As Date = Now
                    Dim dateTaken As Date = #1/1/2099#

                    Dim granularity As FlickrNet.DateGranularity = DateGranularity.FullDate

                    cnt = 0

                    Dim DateNodes As XmlNodeList = xdoc.SelectNodes("//dc:date", xmlns)

                    For Each node7 As XmlNode In DateNodes
                        If node7 IsNot Nothing Then
                            pdate = node7.InnerText
                            If Len(pdate) > 20 Then
                                pdate = ""
                            Else
                                If InStr(pdate, ",") <> 0 Then
                                    pdate = Replace(pdate, ",", "")
                                End If
                                If InStr(pdate, ".") <> 0 Then
                                    pdate = Replace(pdate, ".", "")
                                End If
                                tempDate = pdate
                            End If
                        End If
                    Next

                    If tempDate <> "" Then
                        If InStr(tempDate, "n.d") <> 0 Or InStr(tempDate, "nd") <> 0 Or InStr(tempDate, "unknown") <> 0 Then
                            'do nothing
                        ElseIf InStr(tempDate, "ca") <> 0 Or InStr(tempDate, "cir") <> 0 Then
                            If InStr(tempDate, "-") <> 0 Then
                                Dim r1 = InStr(tempDate, "-")
                                tempDate = Mid(tempDate, r1 - 4, 4)
                            End If

                            If InStr(tempDate, "circa") <> 0 Then
                                tempDate = Replace(tempDate, "circa", "")
                            End If

                            If InStr(tempDate, "cir") <> 0 Then tempDate = Replace(tempDate, "cir", "")
                            If InStr(tempDate, "ca") <> 0 Then tempDate = Replace(tempDate, "ca", "")
                            If InStr(tempDate, ".") <> 0 Then tempDate = Replace(tempDate, ".", "")

                            dateT = "#1/1/" & Trim(tempDate) & "#"
                            dateTaken = dateT
                            granularity = DateGranularity.Circa

                        ElseIf Len(tempDate) = 4 Then
                            dateT = "#1/1/" & Trim(tempDate) & "#"
                            dateTaken = dateT
                            granularity = DateGranularity.YearOnly
                        ElseIf Len(tempDate) = 10 Or InStr(tempDate, " ") <> 0 Then
                            'yyyy-mm-dd --- guessing/hoping (based on Cushman) this will be the standard numerical pattern...
                            If Len(tempDate) = 10 Then
                                Dim l1 = InStr(tempDate, "-")
                                If l1 <> 0 Then
                                    Dim y = Mid(tempDate, 1, 4)
                                    Dim m = Mid(tempDate, 6, 2)
                                    Dim d = Mid(tempDate, 9, 2)
                                    dateT = "#" & m & "/" & d & "/" & y & "#"
                                    dateTaken = dateT
                                    granularity = DateGranularity.FullDate
                                End If
                            Else
                                Dim l1 = InStr(tempDate, " ")
                                If l1 <> 0 Then
                                    Dim m = ""
                                    Dim d = ""
                                    Dim y = ""
                                    Dim tmp1 = Mid(tempDate, 1, l1 - 1)
                                    If Len(tmp1) <> 2 Then
                                        If InStr(tmp1, "Jan") <> 0 Then
                                            m = "01"
                                        ElseIf InStr(tmp1, "Feb") <> 0 Then
                                            m = "02"
                                        ElseIf InStr(tmp1, "Mar") <> 0 Then
                                            m = "03"
                                        ElseIf InStr(tmp1, "Apr") <> 0 Then
                                            m = "04"
                                        ElseIf InStr(tmp1, "May") <> 0 Then
                                            m = "05"
                                        ElseIf InStr(tmp1, "Jun") <> 0 Then
                                            m = "06"
                                        ElseIf InStr(tmp1, "Jul") <> 0 Then
                                            m = "07"
                                        ElseIf InStr(tmp1, "Aug") <> 0 Then
                                            m = "08"
                                        ElseIf InStr(tmp1, "Sep") <> 0 Then
                                            m = "09"
                                        ElseIf InStr(tmp1, "Oct") <> 0 Then
                                            m = "10"
                                        ElseIf InStr(tmp1, "Nov") <> 0 Then
                                            m = "11"
                                        ElseIf InStr(tmp1, "Dec") <> 0 Then
                                            m = "12"
                                        ElseIf Len(tmp1) = 4 Then
                                            y = tmp1
                                        Else
                                            'do nothing -- garbage data...
                                        End If
                                    Else
                                        d = tmp1
                                    End If
                                    Dim tmp2 = Mid(tempDate, l1 + 1)
                                    If InStr(tmp2, " ") <> 0 Then 'full date case
                                        Dim l2 = InStr(tmp2, " ")
                                        If m <> "" Then
                                            d = Mid(tmp2, 1, l2 - 1)
                                            y = Mid(tmp2, l2 + 1)
                                        ElseIf d <> "" Then
                                            If InStr(tmp2, "Jan") <> 0 Then
                                                m = "01"
                                            ElseIf InStr(tmp2, "Feb") <> 0 Then
                                                m = "02"
                                            ElseIf InStr(tmp2, "Mar") <> 0 Then
                                                m = "03"
                                            ElseIf InStr(tmp2, "Apr") <> 0 Then
                                                m = "04"
                                            ElseIf InStr(tmp2, "May") <> 0 Then
                                                m = "05"
                                            ElseIf InStr(tmp2, "Jun") <> 0 Then
                                                m = "06"
                                            ElseIf InStr(tmp2, "Jul") <> 0 Then
                                                m = "07"
                                            ElseIf InStr(tmp2, "Aug") <> 0 Then
                                                m = "08"
                                            ElseIf InStr(tmp2, "Sep") <> 0 Then
                                                m = "09"
                                            ElseIf InStr(tmp2, "Oct") <> 0 Then
                                                m = "10"
                                            ElseIf InStr(tmp2, "Nov") <> 0 Then
                                                m = "11"
                                            ElseIf InStr(tmp2, "Dec") <> 0 Then
                                                m = "12"
                                            Else
                                                'do nothing
                                            End If
                                            y = Mid(tmp2, l2 + 1)
                                        Else
                                            'do nothing -- discounting m/d - yyyy - d/m cases
                                        End If
                                        If d <> "" And m <> "" And y <> "" Then
                                            dateT = "#" & m & "/" & d & "/" & y & "#"
                                            dateTaken = dateT
                                            granularity = DateGranularity.FullDate
                                        End If
                                    ElseIf y <> "" Then 'year then month?
                                        If InStr(tmp2, "Jan") <> 0 Then
                                            m = "01"
                                        ElseIf InStr(tmp2, "Feb") <> 0 Then
                                            m = "02"
                                        ElseIf InStr(tmp2, "Mar") <> 0 Then
                                            m = "03"
                                        ElseIf InStr(tmp2, "Apr") <> 0 Then
                                            m = "04"
                                        ElseIf InStr(tmp2, "May") <> 0 Then
                                            m = "05"
                                        ElseIf InStr(tmp2, "Jun") <> 0 Then
                                            m = "06"
                                        ElseIf InStr(tmp2, "Jul") <> 0 Then
                                            m = "07"
                                        ElseIf InStr(tmp2, "Aug") <> 0 Then
                                            m = "08"
                                        ElseIf InStr(tmp2, "Sep") <> 0 Then
                                            m = "09"
                                        ElseIf InStr(tmp2, "Oct") <> 0 Then
                                            m = "10"
                                        ElseIf InStr(tmp2, "Nov") <> 0 Then
                                            m = "11"
                                        ElseIf InStr(tmp2, "Dec") <> 0 Then
                                            m = "12"
                                        Else
                                            'do nothing
                                        End If
                                        dateT = "#" & m & "/1/" & y & "#"
                                        dateTaken = dateT
                                        granularity = DateGranularity.YearMonthOnly
                                    Else
                                        y = tmp2
                                        dateT = "#" & m & "/1/" & y & "#"
                                        dateTaken = dateT
                                        granularity = DateGranularity.YearMonthOnly
                                    End If
                                End If
                            End If
                        End If
                    End If
                    Console.Out.WriteLine("Date taken: " & dateTaken)

                    'f2.PhotosSetDates
                    If dateTaken = #1/1/2099# Then
                        f2.PhotosSetDates(photoId, datePost)
                    Else
                        f2.PhotosSetDates(photoId, datePost, dateTaken, granularity)
                    End If

                    f2.PhotosLicensesSetLicense(photoId, LicenseType.AttributionCC)
                    f2.PhotosSetPerms(photoId, True, False, False, PermissionComment.Everybody, PermissionAddMeta.Everybody)

                    Dim fRec = New Flickr

                    fRec.identifier = identifier
                    fRec.license = LicenseType.AttributionCC
                    fRec.photoID = photoId
                    fRec.tags = tags
                    fRec.title = title
                    fRec.oai_datestamp = datestamp
                    fRec.dateUpload = Now
                    fRec.date_taken = pdate
                    fRec.description = predescription
                    fRec.creator = creator

                    dbo.Flickrs.InsertOnSubmit(fRec)
                    dbo.SubmitChanges()

                    Console.Out.WriteLine("Photo uploaded successfully.")
                    Console.Out.WriteLine("PhotoID = " & photoId)

                Else

                    Console.Out.WriteLine("Found a non-photo file")

                End If

            Next

        End If

        Console.Out.WriteLine("Press [Enter] to end session")
        Console.In.ReadLine()

    End Sub

End Module
