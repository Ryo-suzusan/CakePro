from fastapi import FastAPI
from pydantic import BaseModel
import compress_fasttext
from scipy.spatial.distance import cosine
import json
from typing import List
import math

app = FastAPI(title="トッピング判定 相加・相乗平均を使用")

model = compress_fasttext.CompressedFastTextKeyedVectors.load(
    "cc.ja.300.compressed.bin"
)
print("モデル読み込み完了")

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
    for word in words:
        try:
            vec = model[word]
            asset_data.append({"asset_id": asset_id, "word": word, "vector": vec})
        except KeyError:
            continue


class UserInput(BaseModel):
    words: List[str]


@app.post("/search", summary="入力された単語リストからアセットIDを検索します")
def search_topping(user_input: UserInput):
    results = []
    asset_scores = []

    # 入力単語リストをカンマ区切りの文字列にする
    input_words_str = ", ".join(user_input.words)

    for item in dictionary:
        asset_id = item["name"]
        asset_words = [w.strip() for w in item["description"].split(",") if w.strip()]
        item_tags = item.get("tags", [])
        normalized_best_scores = []

        for input_word in user_input.words:
            max_sim = -1.0

            for asset_word in asset_words:
                try:
                    sim = model.similarity(input_word, asset_word)
                    if math.isnan(sim):
                        sim = -1.0
                except KeyError:
                    sim = -1.0
                if sim > max_sim:
                    max_sim = sim

            norm_sim = (max_sim + 1.0) / 2.0
            normalized_best_scores.append(norm_sim)

        arithmetic_mean_norm = sum(normalized_best_scores) / len(normalized_best_scores)

        geometric_mean_norm = math.prod(normalized_best_scores) ** (
            1.0 / len(normalized_best_scores)
        )

        arithmetic_mean = arithmetic_mean_norm * 2.0 - 1.0
        geometric_mean = geometric_mean_norm * 2.0 - 1.0

        asset_scores.append(
            {
                "asset_id": asset_id,
                "score": 0,
                "arithmetic_mean": round(float(arithmetic_mean), 3),
                "geometric_mean": round(float(geometric_mean), 3),
                "tags": item_tags,
            }
        )

    sorted_by_ari_assets = sorted(
        asset_scores, key=lambda x: x["arithmetic_mean"], reverse=True
    )
    sorted_by_geo_assets = sorted(
        asset_scores, key=lambda x: x["geometric_mean"], reverse=True
    )

    top_3_ari = sorted_by_ari_assets[:3]
    top_3_geo = sorted_by_geo_assets[:3]

    ari_top1 = top_3_ari[0]
    ari_top2 = top_3_ari[1]
    ari_top3 = top_3_ari[2]

    geo_top1 = top_3_geo[0]
    geo_top2 = top_3_geo[1]
    geo_top3 = top_3_geo[2]

    print(f"ari_top1: {ari_top1['asset_id']}, ari_score: {ari_top1['arithmetic_mean']}")
    print(f"ari_top2: {ari_top2['asset_id']}, ari_score: {ari_top2['arithmetic_mean']}")
    print(f"ari_top3: {ari_top3['asset_id']}, ari_score: {ari_top3['arithmetic_mean']}")

    print(f"geo_top1: {geo_top1['asset_id']}, geo_score: {geo_top1['geometric_mean']}")
    print(f"geo_top2: {geo_top2['asset_id']}, geo_score: {geo_top2['geometric_mean']}")
    print(f"geo_top3: {geo_top3['asset_id']}, geo_score: {geo_top3['geometric_mean']}")

    # 相加平均に対しての処理
    for item in top_3_ari:
        score = item["arithmetic_mean"]
        asset_id = item["asset_id"]
        item_tags = item.get("tags", [])

        if score >= 0.5:
            results.append(
                {
                    "input_word": input_words_str,
                    "asset_id": asset_id,
                    "score": score,
                    "status": "success",
                    "tags": item_tags,
                }
            )
        else:
            results.append(
                {
                    "input_word": input_words_str,
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
