<html> 
   <script src="/Scripts/jquery-1.9.1.min.js"></script>
    
    <script src="/Scripts/jquery-ui-1.10.3.min.js"></script>
  
   <script src="/Scripts/knockout-3.2.0.js"></script>


  
	  <script src="/Scripts/turn.min.js"></script>
 
   <script src="/Scripts/modernizr.2.6.2.Transform2d.min.js"></script>
   
 <script src="/Scripts/installableViewer.js"></script>

    
 <script type='text/javascript'> $.ui.groupdocsViewer.prototype.applicationPath = 'http://localhost:10686/GDWeb.svc';</script>
    
 <script type='text/javascript'> $.ui.groupdocsViewer.prototype.useHttpHandlers = false;</script>

 <script src="/Scripts/GroupdocsViewer.all.js"></script>



<script>
         // For Html Representation
         $(function () {
             $('#viewerHtmlDiv').groupdocsViewer({
                 //filePath: 'http://groupdocs.com/images/banner/carousel2/signature.png',
                 filePath: 'candy.pdf',
                 zoomToFitWidth: true,
                 showFolderBrowser: true,
                 showHeader: true,
                 showPaging: true,
                 showThumbnails: true,
                 showZoom: true,
                 useHtmlThumbnails: true,
                 width: 800,
                 height: 900,
                 useHtmlBasedEngine: true,
                 showDownload: true,
                 downloadPdfFile: true,
                 showPrint: true,
                 usePdfPrinting: true,
                 supportPageRotation: true
                 
             });
         });
         //For Image Representation
         $(function () {
             $('#viewerImageDiv').groupdocsViewer({
                 filePath: 'demo.docx',
                 zoomToFitWidth: true,
                 showFolderBrowser: true,
                 showThumbnails: true,
                 showZoom: true,
                 showHeader: true,
                 showPaging: true,
                 width: 650,
                 height: 900,
                 watermarkPosition: 'Diagonal',
                 watermarkText: '??',
                 watermarkWidth: 10,
                 useHtmlBasedEngine: false,
                 showDownload: true,
                 downloadPdfFile: false,
                 showPrint: true,
                 usePdfPrinting: false
             });
         });
    </script>
</html>  

