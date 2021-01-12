/*
This file is part of the iText (R) project.
Copyright (c) 1998-2021 iText Group NV
Authors: iText Software.

This program is offered under a commercial and under the AGPL license.
For commercial licensing, contact us at https://itextpdf.com/sales.  For AGPL licensing, see below.

AGPL licensing:
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.IO;
using Common.Logging;
using iText.IO.Font.Otf;
using iText.IO.Image;
using iText.IO.Util;
using iText.Kernel.Counter.Event;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Layer;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Font;
using iText.Layout.Properties;
using iText.Pdfa;
using iText.Pdfocr.Events;

namespace iText.Pdfocr {
    /// <summary>
    /// <see cref="OcrPdfCreator"/>
    /// is the class that creates PDF documents containing input
    /// images and text that was recognized using provided
    /// <see cref="IOcrEngine"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="OcrPdfCreator"/>
    /// is the class that creates PDF documents containing input
    /// images and text that was recognized using provided
    /// <see cref="IOcrEngine"/>.
    /// <see cref="OcrPdfCreator"/>
    /// provides possibilities to set list of input images to
    /// be used for OCR, to set scaling mode for images, to set color of text in
    /// output PDF document, to set fixed size of the PDF document's page and to
    /// perform OCR using given images and to return
    /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
    /// as result.
    /// OCR is based on the provided
    /// <see cref="IOcrEngine"/>
    /// (e.g. tesseract reader). This parameter is obligatory and it should be
    /// provided in constructor
    /// or using setter.
    /// </remarks>
    public class OcrPdfCreator {
        /// <summary>The logger.</summary>
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(iText.Pdfocr.OcrPdfCreator));

        /// <summary>Indices in array representing bbox.</summary>
        private const int LEFT_IDX = 0;

        private const int TOP_IDX = 1;

        private const int RIGHT_IDX = 2;

        private const int BOTTOM_IDX = 3;

        /// <summary>
        /// Selected
        /// <see cref="IOcrEngine"/>.
        /// </summary>
        private IOcrEngine ocrEngine;

        /// <summary>Set of properties.</summary>
        private OcrPdfCreatorProperties ocrPdfCreatorProperties;

        /// <summary>
        /// Creates a new
        /// <see cref="OcrPdfCreator"/>
        /// instance.
        /// </summary>
        /// <param name="ocrEngine">
        /// 
        /// <see cref="IOcrEngine"/>
        /// selected OCR Reader
        /// </param>
        public OcrPdfCreator(IOcrEngine ocrEngine)
            : this(ocrEngine, new OcrPdfCreatorProperties()) {
        }

        /// <summary>
        /// Creates a new
        /// <see cref="OcrPdfCreator"/>
        /// instance.
        /// </summary>
        /// <param name="ocrEngine">
        /// selected OCR Reader
        /// <see cref="IOcrEngine"/>
        /// </param>
        /// <param name="ocrPdfCreatorProperties">
        /// set of properties for
        /// <see cref="OcrPdfCreator"/>
        /// </param>
        public OcrPdfCreator(IOcrEngine ocrEngine, OcrPdfCreatorProperties ocrPdfCreatorProperties) {
            SetOcrEngine(ocrEngine);
            SetOcrPdfCreatorProperties(ocrPdfCreatorProperties);
        }

        /// <summary>
        /// Gets properties for
        /// <see cref="OcrPdfCreator"/>.
        /// </summary>
        /// <returns>
        /// set properties
        /// <see cref="OcrPdfCreatorProperties"/>
        /// </returns>
        public OcrPdfCreatorProperties GetOcrPdfCreatorProperties() {
            return ocrPdfCreatorProperties;
        }

        /// <summary>
        /// Sets properties for
        /// <see cref="OcrPdfCreator"/>.
        /// </summary>
        /// <param name="ocrPdfCreatorProperties">
        /// set of properties
        /// <see cref="OcrPdfCreatorProperties"/>
        /// for
        /// <see cref="OcrPdfCreator"/>
        /// </param>
        public void SetOcrPdfCreatorProperties(OcrPdfCreatorProperties ocrPdfCreatorProperties) {
            this.ocrPdfCreatorProperties = ocrPdfCreatorProperties;
        }

        /// <summary>
        /// Performs OCR with set parameters using provided
        /// <see cref="IOcrEngine"/>
        /// and
        /// creates PDF using provided
        /// <see cref="iText.Kernel.Pdf.PdfWriter"/>
        /// and
        /// <see cref="iText.Kernel.Pdf.PdfOutputIntent"/>.
        /// </summary>
        /// <remarks>
        /// Performs OCR with set parameters using provided
        /// <see cref="IOcrEngine"/>
        /// and
        /// creates PDF using provided
        /// <see cref="iText.Kernel.Pdf.PdfWriter"/>
        /// and
        /// <see cref="iText.Kernel.Pdf.PdfOutputIntent"/>.
        /// PDF/A-3u document will be created if
        /// provided
        /// <see cref="iText.Kernel.Pdf.PdfOutputIntent"/>
        /// is not null.
        /// </remarks>
        /// <param name="inputImages">
        /// 
        /// <see cref="System.Collections.IList{E}"/>
        /// of images to be OCRed
        /// </param>
        /// <param name="pdfWriter">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfWriter"/>
        /// object
        /// to write final PDF document to
        /// </param>
        /// <param name="pdfOutputIntent">
        /// 
        /// <see cref="iText.Kernel.Pdf.PdfOutputIntent"/>
        /// for PDF/A-3u document
        /// </param>
        /// <returns>
        /// result PDF/A-3u
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// object
        /// </returns>
        public PdfDocument CreatePdfA(IList<FileInfo> inputImages, PdfWriter pdfWriter, PdfOutputIntent pdfOutputIntent
            ) {
            LOGGER.Info(MessageFormatUtil.Format(PdfOcrLogMessageConstant.START_OCR_FOR_IMAGES, inputImages.Count));
            IMetaInfo storedMetaInfo = null;
            if (ocrEngine is IThreadLocalMetaInfoAware) {
                storedMetaInfo = ((IThreadLocalMetaInfoAware)ocrEngine).GetThreadLocalMetaInfo();
                ((IThreadLocalMetaInfoAware)ocrEngine).SetThreadLocalMetaInfo(new OcrPdfCreatorMetaInfo(((IThreadLocalMetaInfoAware
                    )ocrEngine).GetThreadLocalMetaInfo(), Guid.NewGuid(), null != pdfOutputIntent ? OcrPdfCreatorMetaInfo.PdfDocumentType
                    .PDFA : OcrPdfCreatorMetaInfo.PdfDocumentType.PDF));
            }
            // map contains:
            // keys: image files
            // values:
            // map pageNumber -> retrieved text data(text and its coordinates)
            IDictionary<FileInfo, IDictionary<int, IList<TextInfo>>> imagesTextData = new LinkedDictionary<FileInfo, IDictionary
                <int, IList<TextInfo>>>();
            try {
                foreach (FileInfo inputImage in inputImages) {
                    imagesTextData.Put(inputImage, ocrEngine.DoImageOcr(inputImage));
                }
            }
            finally {
                if (ocrEngine is IThreadLocalMetaInfoAware) {
                    ((IThreadLocalMetaInfoAware)ocrEngine).SetThreadLocalMetaInfo(storedMetaInfo);
                }
            }
            // create PdfDocument
            return CreatePdfDocument(pdfWriter, pdfOutputIntent, imagesTextData);
        }

        /// <summary>
        /// Performs OCR with set parameters using provided
        /// <see cref="IOcrEngine"/>
        /// and
        /// creates PDF using provided
        /// <see cref="iText.Kernel.Pdf.PdfWriter"/>.
        /// </summary>
        /// <param name="inputImages">
        /// 
        /// <see cref="System.Collections.IList{E}"/>
        /// of images to be OCRed
        /// </param>
        /// <param name="pdfWriter">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfWriter"/>
        /// object
        /// to write final PDF document to
        /// </param>
        /// <returns>
        /// result
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// object
        /// </returns>
        public PdfDocument CreatePdf(IList<FileInfo> inputImages, PdfWriter pdfWriter) {
            return CreatePdfA(inputImages, pdfWriter, null);
        }

        /// <summary>
        /// Gets used
        /// <see cref="IOcrEngine"/>.
        /// </summary>
        /// <remarks>
        /// Gets used
        /// <see cref="IOcrEngine"/>.
        /// Returns
        /// <see cref="IOcrEngine"/>
        /// reader object to perform OCR.
        /// </remarks>
        /// <returns>
        /// selected
        /// <see cref="IOcrEngine"/>
        /// instance
        /// </returns>
        public IOcrEngine GetOcrEngine() {
            return ocrEngine;
        }

        /// <summary>
        /// Sets
        /// <see cref="IOcrEngine"/>
        /// reader object to perform OCR.
        /// </summary>
        /// <param name="reader">
        /// selected
        /// <see cref="IOcrEngine"/>
        /// instance
        /// </param>
        public void SetOcrEngine(IOcrEngine reader) {
            ocrEngine = reader;
        }

        /// <summary>Adds image (or its one page) and text that was found there to canvas.</summary>
        /// <param name="pdfDocument">
        /// result
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// </param>
        /// <param name="imageSize">
        /// size of the image according to the selected
        /// <see cref="ScaleMode"/>
        /// </param>
        /// <param name="pageText">text that was found on this image (or on this page)</param>
        /// <param name="imageData">
        /// input image if it is a single page or its one page if
        /// this is a multi-page image
        /// </param>
        /// <param name="createPdfA3u">true if PDF/A3u document is being created</param>
        private void AddToCanvas(PdfDocument pdfDocument, Rectangle imageSize, IList<TextInfo> pageText, ImageData
             imageData, bool createPdfA3u) {
            Rectangle rectangleSize = ocrPdfCreatorProperties.GetPageSize() == null ? imageSize : ocrPdfCreatorProperties
                .GetPageSize();
            PageSize size = new PageSize(rectangleSize);
            PdfPage pdfPage = pdfDocument.AddNewPage(size);
            PdfCanvas canvas = new OcrPdfCreator.NotDefCheckingPdfCanvas(pdfPage, createPdfA3u);
            PdfLayer[] layers = CreatePdfLayers(ocrPdfCreatorProperties.GetImageLayerName(), ocrPdfCreatorProperties.GetTextLayerName
                (), pdfDocument);
            if (layers[0] != null) {
                canvas.BeginLayer(layers[0]);
            }
            AddImageToCanvas(imageData, imageSize, canvas);
            if (layers[0] != null && layers[0] != layers[1]) {
                canvas.EndLayer();
            }
            // how much the original image size changed
            float multiplier = imageData == null ? 1 : imageSize.GetWidth() / PdfCreatorUtil.GetPoints(imageData.GetWidth
                ());
            if (layers[1] != null && layers[0] != layers[1]) {
                canvas.BeginLayer(layers[1]);
            }
            try {
                AddTextToCanvas(imageSize, pageText, canvas, multiplier, pdfPage.GetMediaBox());
            }
            catch (OcrException e) {
                LOGGER.Error(MessageFormatUtil.Format(OcrException.CANNOT_CREATE_PDF_DOCUMENT, e.Message));
                throw new OcrException(OcrException.CANNOT_CREATE_PDF_DOCUMENT).SetMessageParams(e.Message);
            }
            if (layers[1] != null) {
                canvas.EndLayer();
            }
        }

        /// <summary>
        /// Creates a new PDF document using provided properties, adds images with
        /// recognized text.
        /// </summary>
        /// <param name="pdfWriter">
        /// the
        /// <see cref="iText.Kernel.Pdf.PdfWriter"/>
        /// object
        /// to write final PDF document to
        /// </param>
        /// <param name="pdfOutputIntent">
        /// 
        /// <see cref="iText.Kernel.Pdf.PdfOutputIntent"/>
        /// for PDF/A-3u document
        /// </param>
        /// <param name="imagesTextData">
        /// map that contains input image files as keys,
        /// and as value: map pageNumber -&gt; text for the page
        /// </param>
        /// <returns>
        /// result
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// object
        /// </returns>
        private PdfDocument CreatePdfDocument(PdfWriter pdfWriter, PdfOutputIntent pdfOutputIntent, IDictionary<FileInfo
            , IDictionary<int, IList<TextInfo>>> imagesTextData) {
            PdfDocument pdfDocument;
            bool createPdfA3u = pdfOutputIntent != null;
            if (createPdfA3u) {
                pdfDocument = new PdfADocument(pdfWriter, PdfAConformanceLevel.PDF_A_3U, pdfOutputIntent, new DocumentProperties
                    ().SetEventCountingMetaInfo(new PdfOcrMetaInfo()));
            }
            else {
                pdfDocument = new PdfDocument(pdfWriter, new DocumentProperties().SetEventCountingMetaInfo(new PdfOcrMetaInfo
                    ()));
            }
            // pdfLang should be set in PDF/A mode
            bool hasPdfLangProperty = ocrPdfCreatorProperties.GetPdfLang() != null && !ocrPdfCreatorProperties.GetPdfLang
                ().Equals("");
            if (createPdfA3u && !hasPdfLangProperty) {
                LOGGER.Error(MessageFormatUtil.Format(OcrException.CANNOT_CREATE_PDF_DOCUMENT, PdfOcrLogMessageConstant.PDF_LANGUAGE_PROPERTY_IS_NOT_SET
                    ));
                throw new OcrException(OcrException.CANNOT_CREATE_PDF_DOCUMENT).SetMessageParams(PdfOcrLogMessageConstant.
                    PDF_LANGUAGE_PROPERTY_IS_NOT_SET);
            }
            // add metadata
            if (hasPdfLangProperty) {
                pdfDocument.GetCatalog().SetLang(new PdfString(ocrPdfCreatorProperties.GetPdfLang()));
            }
            // set title if it is not empty
            if (ocrPdfCreatorProperties.GetTitle() != null) {
                pdfDocument.GetCatalog().SetViewerPreferences(new PdfViewerPreferences().SetDisplayDocTitle(true));
                PdfDocumentInfo info = pdfDocument.GetDocumentInfo();
                info.SetTitle(ocrPdfCreatorProperties.GetTitle());
            }
            // reset passed font provider
            ocrPdfCreatorProperties.GetFontProvider().Reset();
            AddDataToPdfDocument(imagesTextData, pdfDocument, createPdfA3u);
            return pdfDocument;
        }

        /// <summary>Places provided images and recognized text to the result PDF document.</summary>
        /// <param name="imagesTextData">
        /// map that contains input image
        /// files as keys, and as value:
        /// map pageNumber -&gt; text for the page
        /// </param>
        /// <param name="pdfDocument">
        /// result
        /// <see cref="iText.Kernel.Pdf.PdfDocument"/>
        /// </param>
        /// <param name="createPdfA3u">true if PDF/A3u document is being created</param>
        private void AddDataToPdfDocument(IDictionary<FileInfo, IDictionary<int, IList<TextInfo>>> imagesTextData, 
            PdfDocument pdfDocument, bool createPdfA3u) {
            foreach (KeyValuePair<FileInfo, IDictionary<int, IList<TextInfo>>> entry in imagesTextData) {
                try {
                    FileInfo inputImage = entry.Key;
                    IList<ImageData> imageDataList = PdfCreatorUtil.GetImageData(inputImage, ocrPdfCreatorProperties.GetImageRotationHandler
                        ());
                    LOGGER.Info(MessageFormatUtil.Format(PdfOcrLogMessageConstant.NUMBER_OF_PAGES_IN_IMAGE, inputImage.ToString
                        (), imageDataList.Count));
                    IDictionary<int, IList<TextInfo>> imageTextData = entry.Value;
                    if (imageTextData.Keys.Count > 0) {
                        for (int page = 0; page < imageDataList.Count; ++page) {
                            ImageData imageData = imageDataList[page];
                            Rectangle imageSize = PdfCreatorUtil.CalculateImageSize(imageData, ocrPdfCreatorProperties.GetScaleMode(), 
                                ocrPdfCreatorProperties.GetPageSize());
                            if (imageTextData.ContainsKey(page + 1)) {
                                AddToCanvas(pdfDocument, imageSize, imageTextData.Get(page + 1), imageData, createPdfA3u);
                            }
                        }
                    }
                }
                catch (System.IO.IOException e) {
                    LOGGER.Error(MessageFormatUtil.Format(PdfOcrLogMessageConstant.CANNOT_ADD_DATA_TO_PDF_DOCUMENT, e.Message)
                        );
                }
            }
        }

        /// <summary>Places given image to canvas to background to a separate layer.</summary>
        /// <param name="imageData">
        /// input image as
        /// <see cref="System.IO.FileInfo"/>
        /// </param>
        /// <param name="imageSize">
        /// size of the image according to the selected
        /// <see cref="ScaleMode"/>
        /// </param>
        /// <param name="pdfCanvas">canvas to place the image</param>
        private void AddImageToCanvas(ImageData imageData, Rectangle imageSize, PdfCanvas pdfCanvas) {
            if (imageData != null) {
                if (ocrPdfCreatorProperties.GetPageSize() == null) {
                    pdfCanvas.AddImage(imageData, imageSize, false);
                }
                else {
                    Point coordinates = PdfCreatorUtil.CalculateImageCoordinates(ocrPdfCreatorProperties.GetPageSize(), imageSize
                        );
                    Rectangle rect = new Rectangle((float)coordinates.x, (float)coordinates.y, imageSize.GetWidth(), imageSize
                        .GetHeight());
                    pdfCanvas.AddImage(imageData, rect, false);
                }
            }
        }

        /// <summary>Places retrieved text to canvas to a separate layer.</summary>
        /// <param name="imageSize">
        /// size of the image according to the selected
        /// <see cref="ScaleMode"/>
        /// </param>
        /// <param name="pageText">text that was found on this image (or on this page)</param>
        /// <param name="pdfCanvas">canvas to place the text</param>
        /// <param name="multiplier">coefficient to adjust text placing on canvas</param>
        /// <param name="pageMediaBox">page parameters</param>
        private void AddTextToCanvas(Rectangle imageSize, IList<TextInfo> pageText, PdfCanvas pdfCanvas, float multiplier
            , Rectangle pageMediaBox) {
            if (pageText != null && pageText.Count > 0) {
                Point imageCoordinates = PdfCreatorUtil.CalculateImageCoordinates(ocrPdfCreatorProperties.GetPageSize(), imageSize
                    );
                foreach (TextInfo item in pageText) {
                    String line = item.GetText();
                    float bboxWidthPt = GetWidthPt(item, multiplier);
                    float bboxHeightPt = GetHeightPt(item, multiplier);
                    FontProvider fontProvider = GetOcrPdfCreatorProperties().GetFontProvider();
                    String fontFamily = GetOcrPdfCreatorProperties().GetDefaultFontFamily();
                    if (LineNotEmpty(line, bboxHeightPt, bboxWidthPt)) {
                        Document document = new Document(pdfCanvas.GetDocument());
                        document.SetFontProvider(fontProvider);
                        // Scale the text width to fit the OCR bbox
                        float fontSize = PdfCreatorUtil.CalculateFontSize(document, line, fontFamily, bboxHeightPt, bboxWidthPt);
                        float lineWidth = PdfCreatorUtil.GetRealLineWidth(document, line, fontFamily, fontSize);
                        float xOffset = GetXOffsetPt(item, multiplier);
                        float yOffset = GetYOffsetPt(item, multiplier, imageSize);
                        iText.Layout.Canvas canvas = new iText.Layout.Canvas(pdfCanvas, pageMediaBox);
                        canvas.SetFontProvider(fontProvider);
                        Text text = new Text(line).SetHorizontalScaling(bboxWidthPt / lineWidth);
                        Paragraph paragraph = new Paragraph(text).SetMargin(0).SetMultipliedLeading(1.2f);
                        paragraph.SetFontFamily(fontFamily).SetFontSize(fontSize);
                        paragraph.SetWidth(bboxWidthPt * 1.5f);
                        if (ocrPdfCreatorProperties.GetTextColor() != null) {
                            paragraph.SetFontColor(ocrPdfCreatorProperties.GetTextColor());
                        }
                        else {
                            paragraph.SetTextRenderingMode(PdfCanvasConstants.TextRenderingMode.INVISIBLE);
                        }
                        canvas.ShowTextAligned(paragraph, xOffset + (float)imageCoordinates.x, yOffset + (float)imageCoordinates.y
                            , TextAlignment.LEFT);
                        canvas.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Creates layers for image and text according rules set in
        /// <see cref="OcrPdfCreatorProperties"/>.
        /// </summary>
        /// <param name="imageLayerName">name of the image layer</param>
        /// <param name="textLayerName">name of the text layer</param>
        /// <param name="pdfDocument">document to add layers to</param>
        /// <returns>
        /// array of two layers: first layer is for image, second layer is for text.
        /// Elements may be null meaning that layer creation is not requested
        /// </returns>
        private static PdfLayer[] CreatePdfLayers(String imageLayerName, String textLayerName, PdfDocument pdfDocument
            ) {
            if (imageLayerName == null && textLayerName == null) {
                return new PdfLayer[] { null, null };
            }
            else {
                if (imageLayerName == null) {
                    return new PdfLayer[] { null, new PdfLayer(textLayerName, pdfDocument) };
                }
                else {
                    if (textLayerName == null) {
                        return new PdfLayer[] { new PdfLayer(imageLayerName, pdfDocument), null };
                    }
                    else {
                        if (imageLayerName.Equals(textLayerName)) {
                            PdfLayer pdfLayer = new PdfLayer(imageLayerName, pdfDocument);
                            return new PdfLayer[] { pdfLayer, pdfLayer };
                        }
                        else {
                            return new PdfLayer[] { new PdfLayer(imageLayerName, pdfDocument), new PdfLayer(textLayerName, pdfDocument
                                ) };
                        }
                    }
                }
            }
        }

        /// <summary>Get left bound of text chunk.</summary>
        private static float GetLeft(TextInfo textInfo, float multiplier) {
            if (textInfo.GetBboxRect() == null) {
                return textInfo.GetBbox()[LEFT_IDX] * multiplier;
            }
            else {
                return textInfo.GetBboxRect().GetLeft() * multiplier;
            }
        }

        /// <summary>Get right bound of text chunk.</summary>
        private static float GetRight(TextInfo textInfo, float multiplier) {
            if (textInfo.GetBboxRect() == null) {
                return (textInfo.GetBbox()[RIGHT_IDX] + 1) * multiplier - 1;
            }
            else {
                return (textInfo.GetBboxRect().GetRight() + 1) * multiplier - 1;
            }
        }

        /// <summary>Get top bound of text chunk.</summary>
        private static float GetTop(TextInfo textInfo, float multiplier) {
            if (textInfo.GetBboxRect() == null) {
                return textInfo.GetBbox()[TOP_IDX] * multiplier;
            }
            else {
                return textInfo.GetBboxRect().GetTop() * multiplier;
            }
        }

        /// <summary>Get bottom bound of text chunk.</summary>
        private static float GetBottom(TextInfo textInfo, float multiplier) {
            if (textInfo.GetBboxRect() == null) {
                return (textInfo.GetBbox()[BOTTOM_IDX] + 1) * multiplier - 1;
            }
            else {
                return (textInfo.GetBboxRect().GetBottom() + 1) * multiplier - 1;
            }
        }

        /// <summary>Check if line is not empty.</summary>
        private static bool LineNotEmpty(String line, float bboxHeightPt, float bboxWidthPt) {
            return !String.IsNullOrEmpty(line) && bboxHeightPt > 0 && bboxWidthPt > 0;
        }

        /// <summary>Get width of text chunk in points.</summary>
        private static float GetWidthPt(TextInfo textInfo, float multiplier) {
            if (textInfo.GetBboxRect() == null) {
                return PdfCreatorUtil.GetPoints(GetRight(textInfo, multiplier) - GetLeft(textInfo, multiplier));
            }
            else {
                return GetRight(textInfo, multiplier) - GetLeft(textInfo, multiplier);
            }
        }

        /// <summary>Get height of text chunk in points.</summary>
        private static float GetHeightPt(TextInfo textInfo, float multiplier) {
            if (textInfo.GetBboxRect() == null) {
                return PdfCreatorUtil.GetPoints(GetBottom(textInfo, multiplier) - GetTop(textInfo, multiplier));
            }
            else {
                return GetTop(textInfo, multiplier) - GetBottom(textInfo, multiplier);
            }
        }

        /// <summary>Get horizontal text offset in points.</summary>
        private static float GetXOffsetPt(TextInfo textInfo, float multiplier) {
            if (textInfo.GetBboxRect() == null) {
                return PdfCreatorUtil.GetPoints(GetLeft(textInfo, multiplier));
            }
            else {
                return GetLeft(textInfo, multiplier);
            }
        }

        /// <summary>Get vertical text offset in points.</summary>
        private static float GetYOffsetPt(TextInfo textInfo, float multiplier, Rectangle imageSize) {
            if (textInfo.GetBboxRect() == null) {
                return imageSize.GetHeight() - PdfCreatorUtil.GetPoints(GetBottom(textInfo, multiplier));
            }
            else {
                return GetBottom(textInfo, multiplier);
            }
        }

        /// <summary>A handler for PDF canvas that validates existing glyphs.</summary>
        private class NotDefCheckingPdfCanvas : PdfCanvas {
            private readonly bool createPdfA3u;

            public NotDefCheckingPdfCanvas(PdfPage page, bool createPdfA3u)
                : base(page) {
                this.createPdfA3u = createPdfA3u;
            }

            public override PdfCanvas ShowText(GlyphLine text) {
                OcrPdfCreator.ActualTextCheckingGlyphLine glyphLine = new OcrPdfCreator.ActualTextCheckingGlyphLine(text);
                PdfFont currentFont = GetGraphicsState().GetFont();
                bool notDefGlyphsExists = false;
                // default value for error message, it'll be updated with the
                // unicode of the not found glyph
                String message = PdfOcrLogMessageConstant.COULD_NOT_FIND_CORRESPONDING_GLYPH_TO_UNICODE_CHARACTER;
                for (int i = glyphLine.start; i < glyphLine.end; i++) {
                    if (IsNotDefGlyph(currentFont, glyphLine.Get(i))) {
                        notDefGlyphsExists = true;
                        message = MessageFormatUtil.Format(PdfOcrLogMessageConstant.COULD_NOT_FIND_CORRESPONDING_GLYPH_TO_UNICODE_CHARACTER
                            , glyphLine.Get(i).GetUnicode());
                        if (this.createPdfA3u) {
                            // exception is thrown only if PDF/A document is
                            // being created
                            throw new OcrException(message);
                        }
                        // setting actual text to NotDef glyph
                        glyphLine.SetActualTextToGlyph(i, glyphLine.ToUnicodeString(i, i + 1));
                        // setting a fake unicode deliberately to pass further
                        // checks for actual text necessity during iterating over
                        // glyphline chunks with ActualTextIterator
                        Glyph glyph = new Glyph(glyphLine.Get(i));
                        glyph.SetUnicode(-1);
                        glyphLine.Set(i, glyph);
                    }
                }
                // Warning is logged if not PDF/A document is being created
                if (notDefGlyphsExists) {
                    LOGGER.Warn(message);
                }
                return this.ShowText(glyphLine, new ActualTextIterator(glyphLine));
            }

            private static bool IsNotDefGlyph(PdfFont font, Glyph glyph) {
                if (font is PdfType0Font || font is PdfTrueTypeFont) {
                    return glyph.GetCode() == 0;
                }
                else {
                    if (font is PdfType1Font || font is PdfType3Font) {
                        return glyph.GetCode() == -1;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// A handler for GlyphLine that checks existing actual text not to
        /// overwrite it.
        /// </summary>
        private class ActualTextCheckingGlyphLine : GlyphLine {
            public ActualTextCheckingGlyphLine(GlyphLine other)
                : base(other) {
            }

            public virtual void SetActualTextToGlyph(int i, String text) {
                // set actual text if it doesn't exist for i-th glyph
                if ((this.actualText == null || this.actualText.Count <= i || this.actualText[i] == null)) {
                    base.SetActualText(i, i + 1, text);
                }
            }
        }
    }
}
