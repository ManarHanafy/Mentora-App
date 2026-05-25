# Testing Guide - Exercise Suggestion Refactoring

## 🧪 How to Test the Changes

### Test Scenario 1: Different Messages = Different Exercises

#### Setup
1. Ensure you're using **RealAIService** (default configuration)
2. Run the application
3. Create two user accounts (or use one user for sequential tests)

#### Test Steps

**Step 1: Submit First Journal Entry**
```bash
POST /api/journals
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json

{
  "content": "I feel anxious about my work deadline. I'm having panic attacks."
}
```

**Expected Response:**
```json
{
  "id": 1,
  "userId": 1,
  "content": "I feel anxious about my work deadline. I'm having panic attacks.",
  "createdAt": "2026-04-20T10:00:00Z",
  "riskLevel": "elevated",
  "tags": ["anxiety", "panic"],
  "suggestedExercises": [
    {
      "exerciseCode": "BREATHING_EXERCISE",
      "parameter": "anx",
      "score": 5,
      "scoreRange": "4-8"
    },
    {
      "exerciseCode": "GROUNDING_TECHNIQUE",
      "parameter": "anx",
      "score": 5,
      "scoreRange": "4-8"
    },
    {
      "exerciseCode": "PROGRESSIVE_MUSCLE_RELAXATION",
      "parameter": "str",
      "score": 4,
      "scoreRange": "4-8"
    }
  ]
}
```

**Note:** The exact exercises depend on what the Mentora AI API returns. These are examples.

**Step 2: Query Database to Verify**
```sql
SELECT * FROM SuggestedExercises WHERE JournalEntryId = 1;
```

**Expected Result:**
```
Id | UserId | JournalEntryId | ExerciseCode                    | Parameter | Score
1  | 1      | 1              | BREATHING_EXERCISE              | anx       | 5
2  | 1      | 1              | GROUNDING_TECHNIQUE             | anx       | 5
3  | 1      | 1              | PROGRESSIVE_MUSCLE_RELAXATION   | str       | 4
```

---

**Step 3: Submit Second Journal Entry (Different Content)**
```bash
POST /api/journals
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json

{
  "content": "I couldn't sleep last night. I'm exhausted and my mind won't stop racing."
}
```

**Expected Response:**
```json
{
  "id": 2,
  "userId": 1,
  "content": "I couldn't sleep last night. I'm exhausted and my mind won't stop racing.",
  "createdAt": "2026-04-20T10:15:00Z",
  "riskLevel": "normal",
  "tags": ["sleep_issues", "anxiety"],
  "suggestedExercises": [
    {
      "exerciseCode": "SLEEP_HYGIENE_GUIDE",
      "parameter": "slp",
      "score": 3,
      "scoreRange": "0-4"
    },
    {
      "exerciseCode": "MEDITATION_FOR_SLEEP",
      "parameter": "slp",
      "score": 3,
      "scoreRange": "0-4"
    },
    {
      "exerciseCode": "BREATHING_EXERCISE",
      "parameter": "anx",
      "score": 2,
      "scoreRange": "0-4"
    }
  ]
}
```

**Note:** Different exercises! Even though `BREATHING_EXERCISE` appears again, it's now for Journal 2 with a different score.

**Step 4: Query Database Again**
```sql
SELECT * FROM SuggestedExercises WHERE UserId = 1 ORDER BY JournalEntryId;
```

**Expected Result:**
```
Id | UserId | JournalEntryId | ExerciseCode                    | Parameter | Score
1  | 1      | 1              | BREATHING_EXERCISE              | anx       | 5
2  | 1      | 1              | GROUNDING_TECHNIQUE             | anx       | 5
3  | 1      | 1              | PROGRESSIVE_MUSCLE_RELAXATION   | str       | 4
4  | 1      | 2              | SLEEP_HYGIENE_GUIDE             | slp       | 3
5  | 1      | 2              | MEDITATION_FOR_SLEEP            | slp       | 3
6  | 1      | 2              | BREATHING_EXERCISE              | anx       | 2 ← NEW record!
```

### ✅ Verification Points

- [ ] **Independent Records**: BREATHING_EXERCISE appears twice (Id 1 and 6) with different JournalEntryIds
- [ ] **No Updates**: The original BREATHING_EXERCISE record (Id 1) still points to Journal 1
- [ ] **Different Scores**: Exercise score for Journal 1 (5) is different from Journal 2 (2)
- [ ] **Fresh Suggestions**: Each journal has its own set of exercises based on message content

---

### Test Scenario 2: Same Message Content = Consistent Exercises

#### Purpose
Verify that the AI provides consistent suggestions for the same message (deterministic behavior)

**Step 1: Submit Journal Entry**
```bash
POST /api/journals
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json

{
  "content": "I feel sad and unmotivated."
}
```

**Record the Response:** Note all exercises and their scores

**Step 2: Get Journal by ID (Verify Persistence)**
```bash
GET /api/journals/3
Authorization: Bearer <JWT_TOKEN>
```

**Expected:** Same exercises and scores as Step 1 response

**Step 3: Query Database**
```sql
SELECT COUNT(*) FROM SuggestedExercises WHERE JournalEntryId = 3;
SELECT * FROM SuggestedExercises WHERE JournalEntryId = 3;
```

**Expected:** Exact same exercises persisted

---

### Test Scenario 3: Update Journal Entry

**Purpose:** Verify that updating a journal entry refreshes exercises from new AI analysis

**Step 1: Get Original Journal**
```bash
GET /api/journals/1
Authorization: Bearer <JWT_TOKEN>
```

**Record Original Exercises:**
```json
[
  {"exerciseCode": "BREATHING_EXERCISE", "score": 5},
  {"exerciseCode": "GROUNDING_TECHNIQUE", "score": 5},
  {"exerciseCode": "PROGRESSIVE_MUSCLE_RELAXATION", "score": 4}
]
```

**Step 2: Update Journal with Different Content**
```bash
PUT /api/journals/1
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json

{
  "content": "Actually, I feel much better now. Had a great day at work!"
}
```

**Step 3: Check Updated Response**
```json
{
  "id": 1,
  "content": "Actually, I feel much better now. Had a great day at work!",
  "riskLevel": "normal",
  "suggestedExercises": [
    // ← Should be DIFFERENT exercises based on positive content
  ]
}
```

**Step 4: Query Database**
```sql
SELECT COUNT(*) FROM SuggestedExercises WHERE JournalEntryId = 1;
```

**Expected:** Record count might be different (new exercises added, old ones cleared or replaced based on implementation)

---

### 🔍 Debugging Tips

**To see what the AI API is returning:**
1. Check application logs (Serilog)
2. Look for: `"Calling AI API for journal {JournalId}"`
3. Look for: `"AI API responded for journal {JournalId}"`

**To manually test RealAIService:**
```bash
curl -X POST "https://mentorrra.pythonanywhere.com/analyze" \
  -H "Content-Type: application/json" \
  -d '{
    "journal_text": "I feel anxious",
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
  }'
```

**To verify which AI service is running:**
1. Check `appsettings.json` for `MentoraAI:BaseUrl`
2. Check `DependencyInjection.cs` for `AddAIHttpClient` call
3. Logs will show "Calling AI API" for RealAIService
4. Logs will NOT show API calls if using MockAIService

---

### 📊 Expected Metrics After Refactoring

**Before Refactoring:**
- Same exercises appearing repeatedly across journal entries
- Exercise records getting updated/reassigned to newer journals
- Limited variety in suggestions despite different message content

**After Refactoring:**
- ✅ Unique exercises for each journal entry (at least when content differs)
- ✅ Each exercise has consistent JournalEntryId
- ✅ Exercise variety based on message content analysis
- ✅ No cross-journal deduplication

---

### 🐛 Common Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| Same exercises every time | MockAIService in use | Check DependencyInjection.cs - ensure RealAIService is registered |
| API timeout errors | Mentora AI API slow/down | Check `MentoraAI:TimeoutSeconds` in appsettings.json |
| No exercises returned | AI returned empty list | Check AI response logs, contact Mentora API support |
| Database constraint errors | Schema mismatch | Run `dotnet ef database update` |

---

### ✅ Acceptance Criteria

After the refactoring, verify:

- [ ] Journal Entry 1 and Entry 2 have **different** exercise records in the database
- [ ] BREATHING_EXERCISE appears for both journals as **separate** records
- [ ] Each exercise record has the correct JournalEntryId
- [ ] No exercise record is shared/updated across multiple journals
- [ ] Exercises match the content of each specific journal entry
- [ ] API responses include all exercises from the AI analysis
- [ ] No "semi-fixed" pattern where same exercises appear in same order
