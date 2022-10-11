/*
This file is part of the iText (R) project.
Copyright (c) 1998-2022 iText Group NV
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
using iText.Commons.Actions;
using iText.Commons.Actions.Confirmations;
using iText.Commons.Actions.Contexts;
using iText.Commons.Actions.Sequence;
using iText.Pdfocr.Statistics;

namespace iText.Pdfocr {
    internal class OcrPdfCreatorEventHelper : AbstractPdfOcrEventHelper {
        private readonly SequenceId sequenceId;

        private readonly IMetaInfo metaInfo;

        internal OcrPdfCreatorEventHelper(SequenceId sequenceId, IMetaInfo metaInfo) {
            this.sequenceId = sequenceId;
            this.metaInfo = metaInfo;
        }

        public override void OnEvent(AbstractProductITextEvent @event) {
            if (@event is AbstractContextBasedITextEvent) {
                ((AbstractContextBasedITextEvent)@event).SetMetaInfo(this.metaInfo);
            }
            else {
                if (@event is PdfOcrOutputTypeStatisticsEvent) {
                    // do nothing as we would
                    return;
                }
            }
            EventManager.GetInstance().OnEvent(@event);
        }

        public override SequenceId GetSequenceId() {
            return sequenceId;
        }

        public override EventConfirmationType GetConfirmationType() {
            return EventConfirmationType.ON_CLOSE;
        }
    }
}
