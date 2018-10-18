rm -r hawk/docs
mkdir hawk/docs
python xaml2md.py
cd hawk;
mkdocs serve
