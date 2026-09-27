from fastapi import FastAPI
from pydantic import BaseModel
import compress_fasttext
from scipy.spatial.distance import cosine
import json
from typing import List
import math

app = FastAPI(title="🍰 トッピング判定 AIプロトタイプ (FastText)")

print("モデル読み込み中...")
model = compress_fasttext.CompressedFastTextKeyedVectors.load(
    "cc.ja.300.compressed.bin"
)
print("読み込み完了！")

try:
    with open("dictionary.json", "r", encoding="utf-8") as f:
        dictionary = json.load(f)
except FileNotFoundError:
    print("エラー: dictionary.json が見つかりません。")
    dictionary = []

# 辞書単語のベクトルを事前計算してキャッシュ
asset_data = []
for item in dictionary:
    asset_id = item["name"]
    words = [w.strip() for w in item["description"].split(",") if w.strip()]
    item_tags = item.get("tags", [])
    for word in words:
        try:
            vec = model[word]
            asset_data.append({"asset_id": asset_id, "word": word, "vector": vec, "tags": item_tags})
        except KeyError:
            continue


class UserInput(BaseModel):
    words: List[str]


@app.post("/search", summary="入力された単語リストからアセットIDを検索します")
def search_topping(user_input: UserInput):
    results = []

    # 各アセットの最大類似度と該当単語、タグを保持
    asset_best_scores = {}
    asset_best_words = {}
    asset_best_tags = {}

    # 入力単語ごとに類似度を計算
    for word in user_input.words:
        player_vec = model[word]
        print(f"word: {word}, vector: {player_vec}")
        for item in asset_data:
            sim = model.similarity(word, item["word"])
            print(f"sim: {sim}")

            if math.isnan(sim):
                continue

            asset_id = item["asset_id"]

            # 最高スコアを更新
            if asset_id not in asset_best_scores or sim > asset_best_scores[asset_id]:
                asset_best_scores[asset_id] = sim
                asset_best_words[asset_id] = word
                asset_best_tags[asset_id] = item["tags"]

    # スコアの降順でソートし、上位3件を抽出
    sorted_assets = sorted(asset_best_scores.items(), key=lambda x: x[1], reverse=True)
    top_3 = sorted_assets[:3]

    # 閾値判定（0.5）(要調整) とレスポンスの生成
    for asset_id, score in top_3:
        score = round(float(score), 3)
        input_word = asset_best_words[asset_id]
        item_tags = asset_best_tags.get(asset_id, [])

        if score >= 0.5:
            results.append(
                {
                    "input_word": input_word,
                    "asset_id": asset_id,
                    "score": score,
                    "status": "success",
                    "tags": item_tags,
                }
            )
        else:
            results.append(
                {
                    "input_word": input_word,
                    "asset_id": "error_object",
                    "score": score,
                    "status": "not_found",
                    "tags": [],
                }
            )

    return {"results": results}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)
