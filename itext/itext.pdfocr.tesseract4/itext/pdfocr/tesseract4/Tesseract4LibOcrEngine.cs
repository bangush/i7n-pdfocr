using System;
using System.Collections.Generic;
using System.IO;
using Common.Logging;
using Tesseract;
using iText.IO.Util;

namespace iText.Pdfocr.Tesseract4 {
    /// <summary>
    /// The implementation of
    /// <see cref="AbstractTesseract4OcrEngine"/>
    /// for tesseract OCR.
    /// </summary>
    /// <remarks>
    /// The implementation of
    /// <see cref="AbstractTesseract4OcrEngine"/>
    /// for tesseract OCR.
    /// This class provides possibilities to use features of "tesseract"
    /// using tess4j.
    /// </remarks>
    public class Tesseract4LibOcrEngine : AbstractTesseract4OcrEngine {
        /// <summary>
        /// <see cref="Tesseract.TesseractEngine"/>
        /// Instance.
        /// </summary>
        /// <remarks>
        /// <see cref="Tesseract.TesseractEngine"/>
        /// Instance.
        /// (depends on OS type)
        /// </remarks>
        private TesseractEngine tesseractInstance = null;

        /// <summary>
        /// Creates a new
        /// <see cref="Tesseract4LibOcrEngine"/>
        /// instance.
        /// </summary>
        /// <param name="tesseract4OcrEngineProperties">set of properteis</param>
        public Tesseract4LibOcrEngine(Tesseract4OcrEngineProperties tesseract4OcrEngineProperties)
            : base(tesseract4OcrEngineProperties) {
            tesseractInstance = TesseractOcrUtil.InitializeTesseractInstance(IsWindows(), null, null, null);
        }

        /// <summary>Gets tesseract instance depending on the OS type.</summary>
        /// <remarks>
        /// Gets tesseract instance depending on the OS type.
        /// If instance is null or it was already disposed, it will be initialized
        /// with parameters.
        /// </remarks>
        /// <returns>
        /// initialized
        /// <see cref="Tesseract.TesseractEngine"/>
        /// instance
        /// </returns>
        public virtual TesseractEngine GetTesseractInstance() {
            return tesseractInstance;
        }

        /// <summary>
        /// Initializes instance of tesseract if it haven't been already
        /// initialized or it have been disposed and sets all the required
        /// properties.
        /// </summary>
        /// <param name="outputFormat">
        /// selected
        /// <see cref="OutputFormat"/>
        /// for tesseract
        /// </param>
        public virtual void InitializeTesseract(OutputFormat outputFormat) {
            if (GetTesseractInstance() == null || TesseractOcrUtil.IsTesseractInstanceDisposed(GetTesseractInstance())
                ) {
                tesseractInstance = TesseractOcrUtil.InitializeTesseractInstance(IsWindows(), GetTessData(), GetLanguagesAsString
                    (), GetTesseract4OcrEngineProperties().GetPathToUserWordsFile());
            }
            GetTesseractInstance().SetVariable("tessedit_create_hocr", outputFormat.Equals(OutputFormat.HOCR) ? "1" : 
                "0");
            GetTesseractInstance().SetVariable("user_defined_dpi", "300");
            if (GetTesseract4OcrEngineProperties().GetPathToUserWordsFile() != null) {
                GetTesseractInstance().SetVariable("load_system_dawg", "0");
                GetTesseractInstance().SetVariable("load_freq_dawg", "0");
                GetTesseractInstance().SetVariable("user_words_suffix", GetTesseract4OcrEngineProperties().GetDefaultUserWordsSuffix
                    ());
                GetTesseractInstance().SetVariable("user_words_file", GetTesseract4OcrEngineProperties().GetPathToUserWordsFile
                    ());
            }
            TesseractOcrUtil.SetTesseractProperties(GetTesseractInstance(), GetTessData(), GetLanguagesAsString(), GetTesseract4OcrEngineProperties
                ().GetPageSegMode(), GetTesseract4OcrEngineProperties().GetPathToUserWordsFile());
        }

        /// <summary>
        /// Performs tesseract OCR using command line tool for the selected page
        /// of input image (by default 1st).
        /// </summary>
        /// <param name="inputImage">
        /// input image
        /// <see cref="System.IO.FileInfo"/>
        /// </param>
        /// <param name="outputFiles">
        /// 
        /// <see cref="System.Collections.IList{E}"/>
        /// of output files
        /// (one per each page)
        /// </param>
        /// <param name="outputFormat">
        /// selected
        /// <see cref="OutputFormat"/>
        /// for tesseract
        /// </param>
        /// <param name="pageNumber">number of page to be processed</param>
        public override void DoTesseractOcr(FileInfo inputImage, IList<FileInfo> outputFiles, OutputFormat outputFormat
            , int pageNumber) {
            try {
                ValidateLanguages(GetTesseract4OcrEngineProperties().GetLanguages());
                InitializeTesseract(outputFormat);
                // if preprocessing is not needed and provided image is tiff,
                // the image will be paginated and separate pages will be OCRed
                IList<String> resultList = new List<String>();
                if (!GetTesseract4OcrEngineProperties().IsPreprocessingImages() && ImagePreprocessingUtil.IsTiffImage(inputImage
                    )) {
                    resultList = GetOcrResultForMultiPage(inputImage, outputFormat);
                }
                else {
                    resultList.Add(GetOcrResultForSinglePage(inputImage, outputFormat, pageNumber));
                }
                // list of result strings is written to separate files
                // (one for each page)
                for (int i = 0; i < resultList.Count; i++) {
                    String result = resultList[i];
                    FileInfo outputFile = i >= outputFiles.Count ? null : outputFiles[i];
                    if (result != null && outputFile != null) {
                        try {
                            using (TextWriter writer = new StreamWriter(new FileStream(outputFile.FullName, FileMode.Create), System.Text.Encoding
                                .UTF8)) {
                                writer.Write(result);
                            }
                        }
                        catch (System.IO.IOException e) {
                            LogManager.GetLogger(GetType()).Error(MessageFormatUtil.Format(Tesseract4LogMessageConstant.CANNOT_WRITE_TO_FILE
                                , e.Message));
                            throw new Tesseract4OcrException(Tesseract4OcrException.TESSERACT_FAILED);
                        }
                    }
                }
            }
            catch (Tesseract4OcrException e) {
                LogManager.GetLogger(GetType()).Error(e.Message);
                throw new Tesseract4OcrException(e.Message, e);
            }
            finally {
                if (tesseractInstance != null) {
                    TesseractOcrUtil.DisposeTesseractInstance(tesseractInstance);
                }
                if (GetTesseract4OcrEngineProperties().GetPathToUserWordsFile() != null) {
                    TesseractHelper.DeleteFile(GetTesseract4OcrEngineProperties().GetPathToUserWordsFile());
                }
            }
        }

        /// <summary>
        /// Gets OCR result from provided multi-page image and returns result as
        /// list of strings for each page.
        /// </summary>
        /// <remarks>
        /// Gets OCR result from provided multi-page image and returns result as
        /// list of strings for each page. This method is used for tiff images
        /// when preprocessing is not needed.
        /// </remarks>
        /// <param name="inputImage">
        /// input image
        /// <see cref="System.IO.FileInfo"/>
        /// </param>
        /// <param name="outputFormat">
        /// selected
        /// <see cref="OutputFormat"/>
        /// for tesseract
        /// </param>
        /// <returns>
        /// list of result string that will be written to a temporary files
        /// later
        /// </returns>
        private IList<String> GetOcrResultForMultiPage(FileInfo inputImage, OutputFormat outputFormat) {
            IList<String> resultList = new List<String>();
            try {
                InitializeTesseract(outputFormat);
                TesseractOcrUtil util = new TesseractOcrUtil();
                util.InitializeImagesListFromTiff(inputImage);
                int numOfPages = util.GetListOfPages().Count;
                for (int i = 0; i < numOfPages; i++) {
                    String result = util.GetOcrResultAsString(GetTesseractInstance(), util.GetListOfPages()[i], outputFormat);
                    resultList.Add(result);
                }
            }
            catch (TesseractException e) {
                String msg = MessageFormatUtil.Format(Tesseract4LogMessageConstant.TESSERACT_FAILED, e.Message);
                LogManager.GetLogger(GetType()).Error(msg);
                throw new Tesseract4OcrException(Tesseract4OcrException.TESSERACT_FAILED);
            }
            finally {
                TesseractOcrUtil.DisposeTesseractInstance(GetTesseractInstance());
            }
            return resultList;
        }

        /// <summary>
        /// Gets OCR result from provided single page image and preprocesses it if
        /// it is needed.
        /// </summary>
        /// <param name="inputImage">
        /// input image
        /// <see cref="System.IO.FileInfo"/>
        /// </param>
        /// <param name="outputFormat">
        /// selected
        /// <see cref="OutputFormat"/>
        /// for tesseract
        /// </param>
        /// <param name="pageNumber">number of page to be OCRed</param>
        /// <returns>result as string that will be written to a temporary file later</returns>
        private String GetOcrResultForSinglePage(FileInfo inputImage, OutputFormat outputFormat, int pageNumber) {
            String result = null;
            FileInfo preprocessed = null;
            try {
                // preprocess if required
                if (GetTesseract4OcrEngineProperties().IsPreprocessingImages()) {
                    preprocessed = new FileInfo(ImagePreprocessingUtil.PreprocessImage(inputImage, pageNumber));
                }
                if (!GetTesseract4OcrEngineProperties().IsPreprocessingImages() || preprocessed == null) {
                    // try to open as buffered image if it's not a tiff image
                    System.Drawing.Bitmap bufferedImage = null;
                    try {
                        try {
                            bufferedImage = ImagePreprocessingUtil.ReadImageFromFile(inputImage);
                        }
                        catch (Exception ex) {
                            LogManager.GetLogger(GetType()).Info(MessageFormatUtil.Format(Tesseract4LogMessageConstant.CANNOT_CREATE_BUFFERED_IMAGE
                                , ex.Message));
                            bufferedImage = ImagePreprocessingUtil.ReadAsPixAndConvertToBufferedImage(inputImage);
                        }
                    }
                    catch (System.IO.IOException ex) {
                        LogManager.GetLogger(GetType()).Info(MessageFormatUtil.Format(Tesseract4LogMessageConstant.CANNOT_READ_INPUT_IMAGE
                            , ex.Message));
                    }
                    if (bufferedImage != null) {
                        try {
                            result = new TesseractOcrUtil().GetOcrResultAsString(GetTesseractInstance(), bufferedImage, outputFormat);
                        }
                        catch (TesseractException e) {
                            LogManager.GetLogger(GetType()).Info(MessageFormatUtil.Format(Tesseract4LogMessageConstant.CANNOT_PROCESS_IMAGE
                                , e.Message));
                        }
                    }
                    if (result == null) {
                        result = new TesseractOcrUtil().GetOcrResultAsString(GetTesseractInstance(), inputImage, outputFormat);
                    }
                }
                else {
                    result = new TesseractOcrUtil().GetOcrResultAsString(GetTesseractInstance(), preprocessed, outputFormat);
                }
            }
            catch (TesseractException e) {
                LogManager.GetLogger(GetType()).Error(MessageFormatUtil.Format(Tesseract4LogMessageConstant.TESSERACT_FAILED
                    , e.Message));
                throw new Tesseract4OcrException(Tesseract4OcrException.TESSERACT_FAILED);
            }
            finally {
                if (preprocessed != null) {
                    TesseractHelper.DeleteFile(preprocessed.FullName);
                }
            }
            return result;
        }
    }
}