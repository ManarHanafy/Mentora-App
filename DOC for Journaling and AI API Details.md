# Mentora AI Journal Analyzer — API Documentation

## Overview

The Mentora AI service analyzes journal entries (in Arabic or English) and scores them across 8 psychological parameters using GPT-4o-mini. It detects emotional patterns, cognitive distortions, and risk levels, then maps scores to suggested exercises.

---

## Base URL
```
https://mentorrra.pythonanywhere.com
```

---

## How It Works

1. The backend sends a journal entry + the user's current scores to the API
2. The API sends the text to GPT-4o-mini along with internal scoring sheets
3. GPT matches the journal text to psychological items and calculates score changes (deltas)
4. The API returns updated scores, matched items, tags, risk level, and suggested exercises
5. The backend stores the new scores and uses them in the next request as `current_scores`

> **Important:** The backend must persist `new_scores` after every response and send them back as `current_scores` in the next request. This is how the user's psychological profile builds up over time.

---

## The 8 Parameters

| Code | Meaning | Direction |
|------|---------|-----------|
| `ANX` | Anxiety & worry | Higher = worse |
| `DEP` | Depression & low mood | Higher = worse |
| `STR` | Stress & pressure | Higher = worse |
| `SLP` | Sleep & energy | Higher = healthier |
| `SOC` | Social connection | Higher = healthier |
| `CDT` | Cognitive distortions | Higher = worse |
| `SAFE` | Safety / self-harm risk | Higher = worse |
| `ENG` | Engagement with healthy habits | Higher = healthier |

---

## Endpoints

---

### 1. Health Check

**GET** `/`

Check if the service is running.

**Request:** No body needed.

**Response:**
```json
{
  "status": "ok",
  "service": "Mentora Journal Analyzer"
}
```

---

### 2. Analyze Journal Entry

**POST** `/analyze`

Analyzes a journal entry and returns updated psychological scores.

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "journal_text": "string (required) — the user's journal entry in Arabic or English",
  "current_scores": {
    "ANX": 0,
    "DEP": 0,
    "STR": 0,
    "SLP": 0,
    "SOC": 0,
    "CDT": 0,
    "SAFE": 0,
    "ENG": 0
  }
}
```

**Notes:**
- `journal_text` is required and cannot be empty
- `current_scores` must include all 8 keys
- For a brand new user, send all scores as `0`
- For returning users, send the `new_scores` from the previous response

---

**Success Response (200):**
```json
{
  "matched_items": [
    {
      "parameter": "ANX",
      "items": [
        {
          "id": "ANX2",
          "intensity_0_3": 2,
          "match_text": "I cannot stop worrying"
        }
      ],
      "reason": "User expresses uncontrollable worry about the future."
    }
  ],
  "deltas": {
    "ANX": 4,
    "DEP": 0,
    "STR": 0,
    "SLP": 0,
    "SOC": 0,
    "CDT": 2,
    "SAFE": 0,
    "ENG": 0
  },
  "new_scores": {
    "ANX": 4,
    "DEP": 0,
    "STR": 0,
    "SLP": 0,
    "SOC": 0,
    "CDT": 2,
    "SAFE": 0,
    "ENG": 0
  },
  "tags": ["worry_loop", "catastrophizing"],
  "risk_level": "normal",
  "suggested_exercises": [
    {
      "id": "EX_ANX_01",
      "parameter": "ANX",
      "score": 4,
      "score_range": "1–5"
    }
  ]
}
```

---

**Response Fields Explained:**

| Field | Type | Description |
|-------|------|-------------|
| `matched_items` | array | Which psychological items were detected in the journal, grouped by parameter |
| `matched_items[].parameter` | string | Parameter code (ANX, DEP, etc.) |
| `matched_items[].items` | array | Specific item IDs matched with their intensity and the matching text |
| `matched_items[].reason` | string | Short explanation of why this parameter was triggered |
| `deltas` | object | How much each score changed from this journal entry (can be negative for SLP/SOC/ENG) |
| `new_scores` | object | Updated scores after applying deltas — **save these and send them next time** |
| `tags` | array | Short labels describing the emotional themes detected (e.g. "worry_loop", "sleep_problems") |
| `risk_level` | string | Safety assessment: `"normal"`, `"elevated"`, or `"crisis"` |
| `suggested_exercises` | array | Exercise IDs recommended based on the new scores and score ranges from the parameter sheets |

---

**Risk Level Values:**

| Value | Meaning |
|-------|---------|
| `"normal"` | No safety concern detected |
| `"elevated"` | Some concerning language, monitor closely |
| `"crisis"` | Self-harm or suicide language detected — trigger emergency response immediately |

> **Critical:** If `risk_level` is `"crisis"`, the app must immediately show crisis resources and alert a responsible party.

---

**Error Responses:**

| Status | Meaning | Example |
|--------|---------|---------|
| `400` | Bad request — missing or invalid fields | `{"error": "journal_text is required"}` |
| `500` | Server error — OpenAI issue or misconfiguration | `{"error": "...error details..."}` |

---

## Example: Full Request & Response

**Request:**
```json
POST /analyze
Content-Type: application/json

{
  "journal_text": "اليوم كنت قلقان جداً ومش قادر أوقف أفكاري، حاسس إن كل حاجة هتبوظ",
  "current_scores": {
    "ANX": 3,
    "DEP": 1,
    "STR": 0,
    "SLP": 2,
    "SOC": 1,
    "CDT": 0,
    "SAFE": 0,
    "ENG": 0
  }
}
```

**Response:**
```json
{
  "matched_items": [
    {
      "parameter": "ANX",
      "items": [
        {"id": "ANX2", "intensity_0_3": 2, "match_text": "مش قادر أوقف أفكاري"}
      ],
      "reason": "User describes uncontrollable anxious thoughts."
    },
    {
      "parameter": "CDT",
      "items": [
        {"id": "CDT3", "intensity_0_3": 2, "match_text": "كل حاجة هتبوظ"}
      ],
      "reason": "Catastrophizing — predicting everything will go wrong."
    }
  ],
  "deltas": {"ANX": 2, "DEP": 0, "STR": 0, "SLP": 0, "SOC": 0, "CDT": 2, "SAFE": 0, "ENG": 0},
  "new_scores": {"ANX": 5, "DEP": 1, "STR": 0, "SLP": 2, "SOC": 1, "CDT": 2, "SAFE": 0, "ENG": 0},
  "tags": ["worry_loop", "catastrophizing"],
  "risk_level": "normal",
  "suggested_exercises": [
    {"id": "EX_ANX_02", "parameter": "ANX", "score": 5, "score_range": "4–7"}
  ]
}
```

---

## Backend Integration Notes

1. **Store `new_scores` per user** in your database after every response
2. **Send `new_scores` as `current_scores`** in the next request for the same user
3. **Always handle `risk_level`** — build a check for `"crisis"` on your side
4. **Journal text can be Arabic or English** — the AI handles both
5. **First entry for a new user** → send all `current_scores` as `0`
6. The API has no authentication — if needed, add an API key header on your side

---

## Tech Stack (for reference)
- Python 3.13 + Flask
- OpenAI GPT-4o-mini
- 8 Excel parameter sheets loaded at startup
- Hosted on PythonAnywhere
