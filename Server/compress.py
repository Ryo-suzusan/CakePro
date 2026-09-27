import compress_fasttext
from gensim.models.fasttext import load_facebook_model

print("cc.ja.300.binをロード...")
original_model = load_facebook_model("cc.ja.300.bin").wv

print("モデルの圧縮を実行...")
small_model = compress_fasttext.prune_ft_freq(original_model, pq=True)

print("圧縮モデルを保存...")
small_model.save("cc.ja.300.compressed.bin")

print("全てのプロセスが終了。")
