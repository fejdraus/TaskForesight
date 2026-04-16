CREATE EXTENSION IF NOT EXISTS age;
CREATE EXTENSION IF NOT EXISTS vector;
LOAD 'age';
SET search_path = ag_catalog, "$user", public;
SELECT create_graph('jira_graph');

CREATE TABLE tasks (
    key TEXT PRIMARY KEY,
    summary TEXT,
    description TEXT,
    issue_type TEXT,
    priority TEXT,
    status TEXT,
    assignee TEXT,
    reporter TEXT,
    components JSONB,
    labels JSONB,
    created_at TIMESTAMPTZ,
    resolved_at TIMESTAMPTZ,
    time_in_open REAL,
    time_in_progress REAL,
    time_in_code_review REAL,
    time_in_testing REAL,
    cycle_time REAL,
    lead_time REAL,
    original_estimate_hours REAL,
    time_spent_hours REAL,
    estimation_accuracy REAL,
    return_count INTEGER DEFAULT 0,
    reopen_count INTEGER DEFAULT 0,
    direct_bugs_count INTEGER DEFAULT 0,
    post_release_bugs_count INTEGER DEFAULT 0,
    bug_fix_time_hours REAL DEFAULT 0,
    real_cost_hours REAL,
    task_category TEXT,
    embedding vector(768),
    collected_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_tasks_embedding ON tasks USING hnsw (embedding vector_cosine_ops) WITH (m = 16, ef_construction = 64);
CREATE INDEX idx_tasks_type ON tasks(issue_type);
CREATE INDEX idx_tasks_category ON tasks(task_category);
CREATE INDEX idx_tasks_assignee ON tasks(assignee);
CREATE INDEX idx_tasks_resolved ON tasks(resolved_at);
CREATE INDEX idx_tasks_components ON tasks USING gin(components);

CREATE TABLE status_transitions (
    id SERIAL PRIMARY KEY,
    task_key TEXT REFERENCES tasks(key),
    from_status TEXT,
    to_status TEXT,
    author TEXT,
    transitioned_at TIMESTAMPTZ,
    duration_hours REAL
);

CREATE INDEX idx_transitions_task ON status_transitions(task_key);

CREATE MATERIALIZED VIEW category_stats AS
SELECT
    task_category AS category,
    COUNT(*) AS sample_count,
    AVG(cycle_time) AS avg_cycle_time,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY cycle_time) AS median_cycle_time,
    AVG(estimation_accuracy) AS avg_estimation_accuracy,
    AVG(post_release_bugs_count) AS avg_post_release_bugs,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY real_cost_hours) AS median_real_cost,
    PERCENTILE_CONT(0.8) WITHIN GROUP (ORDER BY real_cost_hours) AS p80_real_cost
FROM tasks
WHERE resolved_at IS NOT NULL AND resolved_at >= NOW() - INTERVAL '6 months'
GROUP BY task_category;
