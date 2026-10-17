"""Extract supplied PDF sources for requirement review; originals are not modified."""
import sys
from pathlib import Path
import logging
logging.getLogger('pypdf').setLevel(logging.ERROR)

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / '.tools' / 'pdf'))
from pypdf import PdfReader

for pdf_name in ('Đề thi giữa kỳ v2.pdf', 'Reading v07.pdf'):
    source = Path('D:/Downloads') / pdf_name
    reader = PdfReader(source)
    print(f'\nSOURCE: {source} | PAGES: {len(reader.pages)}')
    for number, page in enumerate(reader.pages, start=1):
        if '--middle' in sys.argv and (pdf_name != 'Reading v07.pdf' or number < 10 or number > 18):
            continue
        print(f'\n--- PAGE {number} ---\n{page.extract_text() or "[No embedded text]"}')
