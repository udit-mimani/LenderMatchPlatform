import { useState, useEffect } from 'react';
import axios from 'axios';

// ============================================================
// API CONFIGURATION
// ============================================================
// When running in Docker, API requests go through nginx proxy
// When running locally, connect directly to backend

// const API_BASE_URL = window.location.hostname === 'localhost' && window.location.port === '3000'
//   ? 'http://localhost:8080'  // Local development
//   : '';  // Production (nginx proxy)
const API_BASE_URL = import.meta.env.PROD 
  ? ''                       // Docker: Use relative path (e.g. /api/match) so Nginx handles it
: 'http://localhost:8080'; // Local Dev: Go directly to backend
// ============================================================
// TYPE DEFINITIONS
// ============================================================

type MatchResult = {
  lenderName: string;
  isEligible: boolean;
  qualifiedPrograms: string[];
  bestMatchingProgram: string;
  rejectionReasons: string[];
  programMatchReasons: string[];
  failurePoint: string;
  fitScore: number;
  evaluatedAt: string;
};

type DerivedFeatures = {
  equipmentAgeYears: number | null;
  businessType: string;
  isTrucking: boolean;
  isMedical: boolean;
  isStartup: boolean;
  creditTier: string;
  hasPayNetScore: boolean;
  hasCreditIssues: boolean;
  bankruptcyDischargeYears: number;
  hasComparableDebt: boolean;
  tradeLineCount: number;
  loanSizeCategory: string;
  equipmentCategory: string;
};

type MatchingWorkflowResult = {
  applicationId: number;
  evaluatedAt: string;
  isValid: boolean;
  validationErrors: string[];
  derivedFeatures: DerivedFeatures;
  matches: MatchResult[];
  eligibleCount: number;
  totalEvaluated: number;
};

type LendingProgram = {
  id?: number;
  name: string;
  minAmount: number | null;
  maxAmount: number | null;
  minFico: number | null;
  minPayNet: number | null;
  minTimeInBusinessYears: number | null;
  minRevenue: number | null;
  maxEquipmentAgeYears: number | null;
  excludeTrucking: boolean;
};

type Lender = {
  id?: number;
  name: string;
  restrictedIndustries: string[];
  restrictedStates: string[];
  programs: LendingProgram[];
};

type Statistics = {
  totalApplications: number;
  totalLenders: number;
  totalPrograms: number;
  generatedAt: string;
};

type ValidationResult = {
  isValid: boolean;
  errors: string[];
  derivedFeatures: DerivedFeatures;
  validatedAt: string;
};

// ============================================================
// MAIN APP COMPONENT
// ============================================================

export default function App() {
  const [activeView, setActiveView] = useState<'dashboard' | 'application' | 'results' | 'history' | 'policies'>('dashboard');
  const [workflowResult, setWorkflowResult] = useState<MatchingWorkflowResult | null>(null);
  const [lenders, setLenders] = useState<Lender[]>([]);
  const [statistics, setStatistics] = useState<Statistics | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [savedFormData, setSavedFormData] = useState<any>(null);

  // Application form state
  const [formData, setFormData] = useState({
    business: {
      businessName: "Acme Construction Inc",
      industry: "Construction",
      state: "TX",
      yearsInBusiness: 8,
      annualRevenue: 2500000
    },
    guarantor: {
      name: "John Smith",
      ficoScore: 720,
      hasBankruptcy: false,
      hasTaxLiens: false,
      bankruptcyDischargeYears: 0
    },
    creditProfile: {
      payNetScore: 685,
      tradeLineCount: 5,
      hasComparableDebt: true
    },
    request: {
      amount: 150000,
      termMonths: 60,
      equipmentType: "Excavator",
      equipmentYear: 2022,
      equipmentMileage: null
    }
  });

  // Load lenders and statistics on mount
  useEffect(() => {
    fetchLenders();
    fetchStatistics();
  }, []);

  const fetchStatistics = async () => {
    try {
      const res = await axios.get(`${API_BASE_URL}/api/match/statistics`);
      setStatistics(res.data);
    } catch (err) {
      console.error('Error fetching statistics:', err);
    }
  };

  const fetchLenders = async () => {
    try {
      const res = await axios.get(`${API_BASE_URL}/api/match/lenders`);
      setLenders(res.data);
    } catch (err) {
      console.error('Error fetching lenders:', err);
    }
  };

  const updateField = (section: string, field: string, value: any) => {
    setFormData(prev => ({
      ...prev,
      [section]: {
        ...prev[section as keyof typeof prev],
        [field]: value
      }
    }));
  };

  const handleSubmit = async (e: any) => {
    e.preventDefault();
    setIsLoading(true);
    
    try {
      const res = await axios.post(`${API_BASE_URL}/api/match`, formData);
      setWorkflowResult(res.data);
      setSavedFormData({...formData});
      setActiveView('results');
      // Refresh statistics after new submission
      fetchStatistics();
    } catch (err: any) {
      if (err.response?.data?.validationErrors) {
        alert('Validation Errors:\n' + err.response.data.validationErrors.join('\n'));
      } else {
        alert('Error connecting to backend: ' + (err.message || 'Unknown error'));
      }
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleValidate = async () => {
    setIsLoading(true);
    try {
      const res = await axios.post(`${API_BASE_URL}/api/match/validate`, formData);
      const validation: ValidationResult = res.data;
      if (validation.isValid) {
        alert(`✓ Application is valid!\n\nDerived Features:\n• Credit Tier: ${validation.derivedFeatures.creditTier}\n• Business Type: ${validation.derivedFeatures.businessType}\n• Loan Category: ${validation.derivedFeatures.loanSizeCategory}`);
      } else {
        alert('Validation Errors:\n' + validation.errors.join('\n'));
      }
    } catch (err: any) {
      alert('Error validating: ' + (err.message || 'Unknown error'));
    } finally {
      setIsLoading(false);
    }
  };

  const handleReEvaluate = async (applicationId: number) => {
    setIsLoading(true);
    try {
      const res = await axios.post(`${API_BASE_URL}/api/match/${applicationId}/re-evaluate`);
      setWorkflowResult(res.data);
      setActiveView('results');
    } catch (err: any) {
      alert('Error re-evaluating: ' + (err.message || 'Unknown error'));
    } finally {
      setIsLoading(false);
    }
  };

  const handleLookupApplication = async (applicationId: number) => {
    setIsLoading(true);
    try {
      const res = await axios.get(`${API_BASE_URL}/api/match/${applicationId}`);
      const app = res.data;
      // Convert to form data format and re-evaluate
      const appFormData = {
        business: app.business,
        guarantor: app.guarantor,
        creditProfile: app.creditProfile,
        request: app.request
      };
      setSavedFormData(appFormData);
      // Re-evaluate to get matching results
      await handleReEvaluate(applicationId);
    } catch (err: any) {
      if (err.response?.status === 404) {
        alert(`Application #${applicationId} not found`);
      } else {
        alert('Error fetching application: ' + (err.message || 'Unknown error'));
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
      {/* Header */}
      <header className="bg-slate-900/80 backdrop-blur-sm border-b border-slate-700/50 sticky top-0 z-50">
        <div className="max-w-5xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent">
                LenderMatch AI
              </h1>
              <p className="text-slate-400 text-sm mt-1">Intelligent Equipment Finance Matching</p>
            </div>
            
            <nav className="flex gap-1 flex-wrap">
              <button
                onClick={() => { setActiveView('dashboard'); fetchStatistics(); }}
                className={`px-3 py-2 rounded-lg font-medium text-sm ${
                  activeView === 'dashboard'
                    ? 'bg-blue-600 text-white shadow-lg shadow-blue-500/30'
                    : 'text-slate-300 hover:bg-slate-800'
                }`}
              >
                Dashboard
              </button>
              <button
                onClick={() => setActiveView('application')}
                className={`px-3 py-2 rounded-lg font-medium text-sm ${
                  activeView === 'application'
                    ? 'bg-blue-600 text-white shadow-lg shadow-blue-500/30'
                    : 'text-slate-300 hover:bg-slate-800'
                }`}
              >
                New Application
              </button>
              <button
                onClick={() => setActiveView('results')}
                disabled={!workflowResult}
                className={`px-3 py-2 rounded-lg font-medium text-sm ${
                  activeView === 'results'
                    ? 'bg-blue-600 text-white shadow-lg shadow-blue-500/30'
                    : workflowResult
                    ? 'text-slate-300 hover:bg-slate-800'
                    : 'text-slate-600 cursor-not-allowed'
                }`}
              >
                Results
                {workflowResult && (
                  <span className="ml-1 px-1.5 py-0.5 bg-green-500 text-white text-xs rounded-full">
                    {workflowResult.eligibleCount}
                  </span>
                )}
              </button>
              <button
                onClick={() => setActiveView('history')}
                className={`px-3 py-2 rounded-lg font-medium text-sm ${
                  activeView === 'history'
                    ? 'bg-blue-600 text-white shadow-lg shadow-blue-500/30'
                    : 'text-slate-300 hover:bg-slate-800'
                }`}
              >
                Lookup
              </button>
              <button
                onClick={() => { setActiveView('policies'); fetchLenders(); }}
                className={`px-3 py-2 rounded-lg font-medium text-sm ${
                  activeView === 'policies'
                    ? 'bg-blue-600 text-white shadow-lg shadow-blue-500/30'
                    : 'text-slate-300 hover:bg-slate-800'
                }`}
              >
                Lender Policies
              </button>
            </nav>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-5xl mx-auto px-6 py-8">
        {activeView === 'dashboard' && (
          <DashboardView 
            statistics={statistics} 
            onRefresh={fetchStatistics}
            onNewApplication={() => setActiveView('application')}
          />
        )}
        
        {activeView === 'application' && (
          <ApplicationForm
            formData={formData}
            updateField={updateField}
            handleSubmit={handleSubmit}
            handleValidate={handleValidate}
            isLoading={isLoading}
          />
        )}
        
        {activeView === 'results' && workflowResult && (
          <ResultsView 
            workflowResult={workflowResult} 
            formData={savedFormData || formData}
            onReEvaluate={() => handleReEvaluate(workflowResult.applicationId)}
            isLoading={isLoading}
          />
        )}
        
        {activeView === 'history' && (
          <ApplicationLookupView 
            onLookup={handleLookupApplication}
            isLoading={isLoading}
          />
        )}
        
        {activeView === 'policies' && (
          <PoliciesView lenders={lenders} />
        )}
      </main>
    </div>
  );
}

// ============================================================
// DASHBOARD VIEW COMPONENT
// ============================================================

function DashboardView({ statistics, onRefresh, onNewApplication }: { 
  statistics: Statistics | null; 
  onRefresh: () => void;
  onNewApplication: () => void;
}) {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-white">Dashboard</h2>
        <button
          onClick={onRefresh}
          className="px-4 py-2 bg-slate-700 hover:bg-slate-600 text-white rounded-lg text-sm"
        >
          Refresh
        </button>
      </div>

      {/* Statistics Cards */}
      <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
        <h3 className="text-lg font-semibold text-white mb-4">System Statistics</h3>
        {statistics ? (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-700">
                <th className="text-left py-3 text-slate-400 font-medium">Metric</th>
                <th className="text-left py-3 text-slate-400 font-medium">Value</th>
              </tr>
            </thead>
            <tbody>
              <tr className="border-b border-slate-700/50">
                <td className="py-3 text-slate-400">Total Applications Submitted</td>
                <td className="py-3 text-white font-bold text-lg">{statistics.totalApplications}</td>
              </tr>
              <tr className="border-b border-slate-700/50">
                <td className="py-3 text-slate-400">Total Lenders</td>
                <td className="py-3 text-white font-bold text-lg">{statistics.totalLenders}</td>
              </tr>
              <tr className="border-b border-slate-700/50">
                <td className="py-3 text-slate-400">Total Programs</td>
                <td className="py-3 text-white font-bold text-lg">{statistics.totalPrograms}</td>
              </tr>
              <tr>
                <td className="py-3 text-slate-400">Last Updated</td>
                <td className="py-3 text-slate-300 text-sm">{new Date(statistics.generatedAt).toLocaleString()}</td>
              </tr>
            </tbody>
          </table>
        ) : (
          <p className="text-slate-400">Loading statistics...</p>
        )}
      </div>

      {/* Quick Actions */}
      <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
        <h3 className="text-lg font-semibold text-white mb-4">Quick Actions</h3>
        <div className="flex gap-4">
          <button
            onClick={onNewApplication}
            className="px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium"
          >
            Submit New Application
          </button>
        </div>
      </div>
    </div>
  );
}

// ============================================================
// APPLICATION LOOKUP VIEW COMPONENT
// ============================================================

function ApplicationLookupView({ onLookup, isLoading }: { 
  onLookup: (id: number) => void;
  isLoading: boolean;
}) {
  const [applicationId, setApplicationId] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const id = parseInt(applicationId);
    if (id > 0) {
      onLookup(id);
    } else {
      alert('Please enter a valid application ID');
    }
  };

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold text-white">Lookup Application</h2>
      
      <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
        <p className="text-slate-400 mb-4">
          Enter an application ID to retrieve its details and re-evaluate against current lender criteria.
        </p>
        
        <form onSubmit={handleSubmit} className="flex gap-4">
          <input
            type="number"
            min="1"
            placeholder="Application ID (e.g., 1, 2, 3...)"
            value={applicationId}
            onChange={(e) => setApplicationId(e.target.value)}
            className="flex-1 bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-3 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <button
            type="submit"
            disabled={isLoading || !applicationId}
            className="px-6 py-3 bg-blue-600 hover:bg-blue-700 disabled:bg-slate-700 disabled:cursor-not-allowed text-white rounded-lg font-medium"
          >
            {isLoading ? 'Loading...' : 'Lookup & Re-evaluate'}
          </button>
        </form>
      </div>

      <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
        <h3 className="text-lg font-semibold text-white mb-3">How it works</h3>
        <table className="w-full text-sm">
          <tbody>
            <tr className="border-b border-slate-700/50">
              <td className="py-3 text-slate-400 w-8">1.</td>
              <td className="py-3 text-slate-300">Enter the application ID from a previous submission</td>
            </tr>
            <tr className="border-b border-slate-700/50">
              <td className="py-3 text-slate-400">2.</td>
              <td className="py-3 text-slate-300">The system retrieves the original application details</td>
            </tr>
            <tr>
              <td className="py-3 text-slate-400">3.</td>
              <td className="py-3 text-slate-300">Re-evaluates against current lender policies (which may have changed)</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ============================================================
// APPLICATION FORM COMPONENT
// ============================================================

function ApplicationForm({ formData, updateField, handleSubmit, handleValidate, isLoading }: any) {
  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Business Information */}
        <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
          <h2 className="text-xl font-bold text-white mb-4 flex items-center gap-2">
            <span className="w-8 h-8 bg-blue-500/20 rounded-lg flex items-center justify-center text-blue-400">
              🏢
            </span>
            Business Information
          </h2>
          
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-300 mb-2">Business Name</label>
              <input
                type="text"
                className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={formData.business.businessName}
                onChange={e => updateField('business', 'businessName', e.target.value)}
                required
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">Industry</label>
                <input
                  type="text"
                  className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={formData.business.industry}
                  onChange={e => updateField('business', 'industry', e.target.value)}
                  placeholder="e.g., Construction"
                  required
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">State</label>
                <input
                  type="text"
                  maxLength={2}
                  className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500 uppercase"
                  value={formData.business.state}
                  onChange={e => updateField('business', 'state', e.target.value.toUpperCase())}
                  placeholder="TX"
                  required
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">Years in Business</label>
                <input
                  type="number"
                  step="0.1"
                  className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={formData.business.yearsInBusiness}
                  onChange={e => updateField('business', 'yearsInBusiness', parseFloat(e.target.value))}
                  required
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">Annual Revenue</label>
                <input
                  type="number"
                  className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={formData.business.annualRevenue}
                  onChange={e => updateField('business', 'annualRevenue', parseFloat(e.target.value))}
                  placeholder="2500000"
                  required
                />
              </div>
            </div>
          </div>
        </div>

        {/* Personal Guarantor */}
        <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
          <h2 className="text-xl font-bold text-white mb-4 flex items-center gap-2">
            <span className="w-8 h-8 bg-green-500/20 rounded-lg flex items-center justify-center text-green-400">
              👤
            </span>
            Personal Guarantor
          </h2>
          
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-300 mb-2">Guarantor Name</label>
              <input
                type="text"
                className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={formData.guarantor.name}
                onChange={e => updateField('guarantor', 'name', e.target.value)}
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-300 mb-2">FICO Score</label>
              <input
                type="number"
                min="300"
                max="850"
                className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={formData.guarantor.ficoScore}
                onChange={e => updateField('guarantor', 'ficoScore', parseInt(e.target.value))}
                required
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    className="w-4 h-4 text-blue-600 bg-slate-900 border-slate-600 rounded focus:ring-blue-500"
                    checked={formData.guarantor.hasBankruptcy}
                    onChange={e => updateField('guarantor', 'hasBankruptcy', e.target.checked)}
                  />
                  <span className="text-sm text-slate-300">Has Bankruptcy</span>
                </label>
              </div>
              
              <div>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    className="w-4 h-4 text-blue-600 bg-slate-900 border-slate-600 rounded focus:ring-blue-500"
                    checked={formData.guarantor.hasTaxLiens}
                    onChange={e => updateField('guarantor', 'hasTaxLiens', e.target.checked)}
                  />
                  <span className="text-sm text-slate-300">Has Tax Liens</span>
                </label>
              </div>
            </div>

            {formData.guarantor.hasBankruptcy && (
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">Years Since Bankruptcy Discharge</label>
                <input
                  type="number"
                  className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={formData.guarantor.bankruptcyDischargeYears}
                  onChange={e => updateField('guarantor', 'bankruptcyDischargeYears', parseInt(e.target.value))}
                />
              </div>
            )}
          </div>
        </div>

        {/* Business Credit */}
        <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
          <h2 className="text-xl font-bold text-white mb-4 flex items-center gap-2">
            <span className="w-8 h-8 bg-purple-500/20 rounded-lg flex items-center justify-center text-purple-400">
              📊
            </span>
            Business Credit Profile
          </h2>
          
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-300 mb-2">PayNet Score (optional)</label>
              <input
                type="number"
                className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={formData.creditProfile.payNetScore || ''}
                onChange={e => updateField('creditProfile', 'payNetScore', e.target.value ? parseInt(e.target.value) : null)}
                placeholder="Leave empty if none"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-300 mb-2">Trade Line Count</label>
              <input
                type="number"
                className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={formData.creditProfile.tradeLineCount}
                onChange={e => updateField('creditProfile', 'tradeLineCount', parseInt(e.target.value))}
                required
              />
            </div>

            <div>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  className="w-4 h-4 text-blue-600 bg-slate-900 border-slate-600 rounded focus:ring-blue-500"
                  checked={formData.creditProfile.hasComparableDebt}
                  onChange={e => updateField('creditProfile', 'hasComparableDebt', e.target.checked)}
                />
                <span className="text-sm text-slate-300">Has Comparable Business Debt</span>
              </label>
            </div>
          </div>
        </div>

        {/* Loan Request */}
        <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
          <h2 className="text-xl font-bold text-white mb-4 flex items-center gap-2">
            <span className="w-8 h-8 bg-yellow-500/20 rounded-lg flex items-center justify-center text-yellow-400">
              💰
            </span>
            Loan Request
          </h2>
          
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">Loan Amount ($)</label>
                <input
                  type="number"
                  className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={formData.request.amount}
                  onChange={e => updateField('request', 'amount', parseFloat(e.target.value))}
                  required
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">Term (months)</label>
                <input
                  type="number"
                  className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={formData.request.termMonths}
                  onChange={e => updateField('request', 'termMonths', parseInt(e.target.value))}
                  required
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-300 mb-2">Equipment Type</label>
              <input
                type="text"
                className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={formData.request.equipmentType}
                onChange={e => updateField('request', 'equipmentType', e.target.value)}
                placeholder="e.g., Excavator"
                required
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">Equipment Year</label>
                <input
                  type="number"
                  className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={formData.request.equipmentYear}
                  onChange={e => updateField('request', 'equipmentYear', parseInt(e.target.value))}
                  required
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">Mileage (optional)</label>
                <input
                  type="number"
                  className="w-full bg-slate-900/50 border border-slate-600 rounded-lg px-4 py-2.5 text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={formData.request.equipmentMileage || ''}
                  onChange={e => updateField('request', 'equipmentMileage', e.target.value ? parseInt(e.target.value) : null)}
                  placeholder="Leave empty if N/A"
                />
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Submit Buttons */}
      <div className="flex justify-center gap-4 pt-4">
        <button
          type="button"
          onClick={handleValidate}
          disabled={isLoading}
          className="px-6 py-3 bg-slate-700 hover:bg-slate-600 text-white font-medium rounded-xl transition-all disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Validate First
        </button>
        <button
          type="submit"
          disabled={isLoading}
          className="px-8 py-4 bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-700 hover:to-cyan-700 text-white font-bold rounded-xl shadow-xl shadow-blue-500/30 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-3"
        >
          {isLoading ? (
            <>
              <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
              </svg>
              Processing...
            </>
          ) : (
            <>
              <span>🎯</span>
              Find Matching Lenders
            </>
          )}
        </button>
      </div>
    </form>
  );
}

// ============================================================
// RESULTS VIEW COMPONENT
// ============================================================

function ResultsView({ workflowResult, formData, onReEvaluate, isLoading }: { 
  workflowResult: MatchingWorkflowResult; 
  formData: any;
  onReEvaluate?: () => void;
  isLoading?: boolean;
}) {
  const [selectedLender, setSelectedLender] = useState<MatchResult | null>(null);

  const eligibleMatches = workflowResult.matches.filter(m => m.isEligible);
  const ineligibleMatches = workflowResult.matches.filter(m => !m.isEligible);

  return (
    <div className="space-y-6">
      {/* Summary Table */}
      <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-bold text-white">Match Summary</h2>
          {onReEvaluate && (
            <button
              onClick={onReEvaluate}
              disabled={isLoading}
              className="px-4 py-2 bg-slate-700 hover:bg-slate-600 disabled:opacity-50 text-white rounded-lg text-sm"
            >
              {isLoading ? 'Re-evaluating...' : 'Re-evaluate'}
            </button>
          )}
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-700">
              <th className="text-left py-3 text-slate-400 font-medium">Metric</th>
              <th className="text-left py-3 text-slate-400 font-medium">Value</th>
            </tr>
          </thead>
          <tbody>
            <tr className="border-b border-slate-700/50">
              <td className="py-3 text-slate-400">Application ID</td>
              <td className="py-3 text-white font-bold">#{workflowResult.applicationId}</td>
            </tr>
            <tr className="border-b border-slate-700/50">
              <td className="py-3 text-slate-400">Evaluated At</td>
              <td className="py-3 text-slate-300 text-sm">{new Date(workflowResult.evaluatedAt).toLocaleString()}</td>
            </tr>
            <tr className="border-b border-slate-700/50">
              <td className="py-3 text-slate-400">Eligible Lenders</td>
              <td className="py-3 text-green-400 font-bold text-lg">{workflowResult.eligibleCount}</td>
            </tr>
            <tr className="border-b border-slate-700/50">
              <td className="py-3 text-slate-400">Ineligible Lenders</td>
              <td className="py-3 text-red-400 font-bold text-lg">{workflowResult.totalEvaluated - workflowResult.eligibleCount}</td>
            </tr>
            <tr className="border-b border-slate-700/50">
              <td className="py-3 text-slate-400">Credit Tier</td>
              <td className="py-3 text-white font-bold text-lg">{workflowResult.derivedFeatures.creditTier}</td>
            </tr>
            <tr>
              <td className="py-3 text-slate-400">Best Fit Score</td>
              <td className="py-3 text-purple-400 font-bold text-lg">{eligibleMatches.length > 0 ? Math.max(...eligibleMatches.map(m => m.fitScore)) : 0}</td>
            </tr>
          </tbody>
        </table>
      </div>

      {/* Application Details */}
      <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
        <h2 className="text-xl font-bold text-white mb-4">Application Summary</h2>
        <table className="w-full text-sm">
          <tbody>
            <tr className="border-b border-slate-700/50">
              <td className="py-3 text-slate-400 w-1/3">Business</td>
              <td className="py-3 text-white font-medium">{formData.business.businessName}</td>
            </tr>
            <tr className="border-b border-slate-700/50">
              <td className="py-3 text-slate-400">Industry</td>
              <td className="py-3 text-white font-medium">{formData.business.industry}</td>
            </tr>
            <tr className="border-b border-slate-700/50">
              <td className="py-3 text-slate-400">Loan Amount</td>
              <td className="py-3 text-white font-medium">${formData.request.amount.toLocaleString()}</td>
            </tr>
            <tr>
              <td className="py-3 text-slate-400">Equipment</td>
              <td className="py-3 text-white font-medium">{formData.request.equipmentType} ({formData.request.equipmentYear})</td>
            </tr>
          </tbody>
        </table>
        
        {/* Derived Features */}
        <div className="mt-6 pt-4 border-t border-slate-700">
          <h3 className="text-sm font-semibold text-slate-300 mb-4">Derived Features</h3>
          <table className="w-full text-sm">
            <tbody>
              <tr className="border-b border-slate-700/50">
                <td className="py-3 text-slate-400 w-1/3">Business Type</td>
                <td className="py-3 text-white font-medium">{workflowResult.derivedFeatures.businessType}</td>
              </tr>
              <tr className="border-b border-slate-700/50">
                <td className="py-3 text-slate-400">Loan Category</td>
                <td className="py-3 text-white font-medium">{workflowResult.derivedFeatures.loanSizeCategory}</td>
              </tr>
              <tr className="border-b border-slate-700/50">
                <td className="py-3 text-slate-400">Equipment Age</td>
                <td className="py-3 text-white font-medium">{workflowResult.derivedFeatures.equipmentAgeYears || 0} years</td>
              </tr>
              <tr className="border-b border-slate-700/50">
                <td className="py-3 text-slate-400">Trade Lines</td>
                <td className="py-3 text-white font-medium">{workflowResult.derivedFeatures.tradeLineCount}</td>
              </tr>
              <tr>
                <td className="py-3 text-slate-400">Status</td>
                <td className="py-3">
                  <span className={`px-2 py-1 rounded text-xs font-medium ${workflowResult.derivedFeatures.isStartup ? 'bg-yellow-500/20 text-yellow-300' : 'bg-green-500/20 text-green-300'}`}>
                    {workflowResult.derivedFeatures.isStartup ? 'Startup' : 'Established'}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      {/* Eligible Lenders */}
      {eligibleMatches.length > 0 && (
        <div>
          <h2 className="text-2xl font-bold text-white mb-4">
            Eligible Lenders ({eligibleMatches.length})
          </h2>
          <div className="space-y-6">
            {eligibleMatches.map((match, idx) => (
              <LenderCard
                key={idx}
                match={match}
                formData={formData}
                isExpanded={selectedLender?.lenderName === match.lenderName}
                onToggle={() => setSelectedLender(selectedLender?.lenderName === match.lenderName ? null : match)}
              />
            ))}
          </div>
        </div>
      )}

      {/* Ineligible Lenders */}
      {ineligibleMatches.length > 0 && (
        <div>
          <h2 className="text-2xl font-bold text-white mb-4">
            Ineligible Lenders ({ineligibleMatches.length})
          </h2>
          <div className="space-y-6">
            {ineligibleMatches.map((match, idx) => (
              <LenderCard
                key={idx}
                match={match}
                formData={formData}
                isExpanded={selectedLender?.lenderName === match.lenderName}
                onToggle={() => setSelectedLender(selectedLender?.lenderName === match.lenderName ? null : match)}
              />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function LenderCard({ match, formData, isExpanded, onToggle }: any) {
  const isEligible = match.isEligible;

  return (
    <div className={`bg-slate-800/50 backdrop-blur-sm rounded-xl border ${
      isEligible ? 'border-green-500/30' : 'border-red-500/30'
    }`}>
      {/* Header */}
      <div
        className="p-6 cursor-pointer hover:bg-slate-700/30"
        onClick={onToggle}
      >
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-xl font-bold text-white">{match.lenderName}</h3>
            {isEligible && match.bestMatchingProgram && (
              <p className="text-sm text-slate-400 mt-1">Best Program: {match.bestMatchingProgram}</p>
            )}
            {!isEligible && match.failurePoint && (
              <p className="text-sm text-red-400 mt-1">Failed at: {match.failurePoint}</p>
            )}
          </div>

          <div className="flex items-center gap-4">
            {isEligible && (
              <div className="text-right">
                <div className="text-3xl font-bold text-white">{match.fitScore}</div>
                <div className="text-xs text-slate-400">Fit Score</div>
              </div>
            )}
            
            <svg
              width="20"
              height="20"
              className={`text-slate-400 flex-shrink-0 ${isExpanded ? 'rotate-180' : ''}`}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              style={{ width: '20px', height: '20px', minWidth: '20px', minHeight: '20px', transition: 'transform 0.2s ease' }}
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </div>
        </div>
      </div>

      {/* Expanded Details */}
      {isExpanded && (
        <div className="px-6 pb-6 space-y-4 border-t border-slate-700">
          {isEligible ? (
            <>
              {/* Qualified Programs */}
              {match.qualifiedPrograms.length > 0 && (
                <div className="mt-4">
                  <h4 className="text-sm font-semibold text-slate-300 mb-2">Qualified Programs</h4>
                  <div className="flex flex-wrap gap-2">
                    {match.qualifiedPrograms.map((prog: string, idx: number) => (
                      <span
                        key={idx}
                        className={`px-3 py-1 rounded-full text-xs font-medium ${
                          prog === match.bestMatchingProgram
                            ? 'bg-green-500 text-white'
                            : 'bg-slate-700 text-slate-300'
                        }`}
                      >
                        {prog}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              {/* Match Reasons */}
              {match.programMatchReasons.length > 0 && (
                <div>
                  <h4 className="text-sm font-semibold text-green-400 mb-3">Why You Qualify</h4>
                  <div className="bg-slate-900/30 rounded-lg border border-slate-700/50 overflow-hidden">
                    <table className="w-full text-sm">
                      <tbody>
                        {match.programMatchReasons.map((reason: string, idx: number) => (
                          <tr key={idx} className="border-b border-slate-700/30 last:border-0">
                            <td className="px-4 py-3 text-slate-300">{reason}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {/* Detailed Criteria Met */}
              <DetailedCriteriaView formData={formData} />
            </>
          ) : (
            <>
              {/* Rejection Reasons */}
              <div className="mt-4">
                <h4 className="text-sm font-semibold text-red-400 mb-3">Rejection Reasons</h4>
                <div className="bg-slate-900/30 rounded-lg border border-slate-700/50 overflow-hidden">
                  <table className="w-full text-sm">
                    <tbody>
                      {match.rejectionReasons.map((reason: string, idx: number) => (
                        <tr key={idx} className="border-b border-slate-700/30 last:border-0">
                          <td className="px-4 py-3 text-slate-300">{reason}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              {/* What Would Have Been Needed */}
              <div>
                <h4 className="text-sm font-semibold text-yellow-400 mb-2">💡 What's Needed to Qualify</h4>
                <div className="bg-yellow-500/10 rounded-lg p-4 text-sm text-slate-300">
                  {getQualificationSuggestions(match, formData)}
                </div>
              </div>
            </>
          )}
        </div>
      )}
    </div>
  );
}

function DetailedCriteriaView({ formData }: any) {
  const criteria = [
    {
      name: 'FICO Score',
      value: formData.guarantor.ficoScore,
    },
    {
      name: 'PayNet Score',
      value: formData.creditProfile.payNetScore || 'Not Provided',
    },
    {
      name: 'Time in Business',
      value: `${formData.business.yearsInBusiness} years`,
    },
    {
      name: 'Loan Amount',
      value: `$${formData.request.amount.toLocaleString()}`,
    },
    {
      name: 'Trade Lines',
      value: formData.creditProfile.tradeLineCount,
    }
  ];

  return (
    <div>
      <h4 className="text-sm font-semibold text-slate-300 mb-3">Criteria Breakdown</h4>
      <div className="bg-slate-900/30 rounded-lg border border-slate-700/50 overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-700">
              <th className="text-left px-4 py-3 text-slate-400 font-medium">Criteria</th>
              <th className="text-left px-4 py-3 text-slate-400 font-medium">Value</th>
            </tr>
          </thead>
          <tbody>
            {criteria.map((item, idx) => (
              <tr key={idx} className="border-b border-slate-700/50 last:border-0">
                <td className="px-4 py-3 text-slate-400">{item.name}</td>
                <td className="px-4 py-3 text-white font-medium">{item.value}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function getQualificationSuggestions(match: MatchResult, formData: any) {
  const suggestions = [];

  if (match.rejectionReasons.some(r => r.includes('FICO'))) {
    const currentFico = formData.guarantor.ficoScore;
    suggestions.push(`Improve FICO score from ${currentFico} to 700+`);
  }

  if (match.rejectionReasons.some(r => r.includes('PayNet'))) {
    suggestions.push('Establish business credit to obtain a PayNet score of 660+');
  }

  if (match.rejectionReasons.some(r => r.includes('time in business'))) {
    suggestions.push(`Continue operating - need ${Math.ceil(2 - formData.business.yearsInBusiness)} more years in business`);
  }

  if (match.rejectionReasons.some(r => r.includes('State') || r.includes('state'))) {
    suggestions.push('This lender does not operate in your state. Consider lenders without state restrictions.');
  }

  if (match.rejectionReasons.some(r => r.includes('Industry') || r.includes('industry'))) {
    suggestions.push('This lender does not finance your industry. Look for industry-specific lenders.');
  }

  if (match.rejectionReasons.some(r => r.includes('bankruptcy'))) {
    const yearsNeeded = 7 - (formData.guarantor.bankruptcyDischargeYears || 0);
    if (yearsNeeded > 0) {
      suggestions.push(`Wait ${yearsNeeded} more years from bankruptcy discharge`);
    }
  }

  if (suggestions.length === 0) {
    suggestions.push('Review specific program requirements or consult with the lender for alternative options.');
  }

  return (
    <ul className="space-y-1">
      {suggestions.map((suggestion, idx) => (
        <li key={idx} className="text-sm">
          {suggestion}
        </li>
      ))}
    </ul>
  );
}

// ============================================================
// POLICIES VIEW COMPONENT
// ============================================================

function PoliciesView({ lenders }: { lenders: Lender[] }) {
  const [selectedLender, setSelectedLender] = useState<Lender | null>(null);
  const [editMode, setEditMode] = useState(false);

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      {/* Lender List */}
      <div className="lg:col-span-1 space-y-4">
        <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
          <h2 className="text-xl font-bold text-white mb-4">Lenders ({lenders.length})</h2>
          <div className="space-y-2">
            {lenders.map((lender, idx) => (
              <button
                key={idx}
                onClick={() => { setSelectedLender(lender); setEditMode(false); }}
                className={`w-full text-left p-4 rounded-lg transition-all ${
                  selectedLender?.name === lender.name
                    ? 'bg-blue-600 text-white'
                    : 'bg-slate-700/50 text-slate-300 hover:bg-slate-700'
                }`}
              >
                <div className="font-semibold">{lender.name}</div>
                <div className="text-xs opacity-75 mt-1">
                  {lender.programs.length} Programs
                </div>
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Lender Details */}
      <div className="lg:col-span-2">
        {selectedLender ? (
          <div className="space-y-6">
            {/* Header */}
            <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-2xl font-bold text-white">{selectedLender.name}</h2>
                <button
                  onClick={() => setEditMode(!editMode)}
                  className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors"
                >
                  {editMode ? 'View Mode' : 'Edit Mode'}
                </button>
              </div>

              {/* Restrictions */}
              <div className="space-y-6">
                <div>
                  <h3 className="text-sm font-semibold text-slate-300 mb-3">Restricted States</h3>
                  {selectedLender.restrictedStates && selectedLender.restrictedStates.length > 0 ? (
                    <div className="bg-slate-900/30 rounded-lg border border-slate-700/50 overflow-hidden">
                      <table className="w-full text-sm">
                        <tbody>
                          {(Array.isArray(selectedLender.restrictedStates) 
                            ? selectedLender.restrictedStates 
                            : String(selectedLender.restrictedStates).split(',').map(s => s.trim())
                          ).map((state: string, idx: number) => (
                            <tr key={idx} className="border-b border-slate-700/30 last:border-0">
                              <td className="px-4 py-2 text-red-300">{state}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <span className="text-slate-500 text-sm">No state restrictions</span>
                  )}
                </div>

                <div className="pt-4 border-t border-slate-600">
                  <h3 className="text-sm font-semibold text-slate-300 mb-3">Restricted Industries</h3>
                  {selectedLender.restrictedIndustries && selectedLender.restrictedIndustries.length > 0 ? (
                    <div className="bg-slate-900/30 rounded-lg border border-slate-700/50 overflow-hidden max-h-64 overflow-y-auto">
                      <table className="w-full text-sm">
                        <tbody>
                          {(Array.isArray(selectedLender.restrictedIndustries) 
                            ? selectedLender.restrictedIndustries 
                            : String(selectedLender.restrictedIndustries).split(',').map(s => s.trim())
                          ).map((industry: string, idx: number) => (
                            <tr key={idx} className="border-b border-slate-700/30 last:border-0">
                              <td className="px-4 py-2 text-red-300">{industry}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <span className="text-slate-500 text-sm">No industry restrictions</span>
                  )}
                </div>
              </div>
            </div>

            {/* Programs */}
            <div>
              <h3 className="text-xl font-bold text-white mb-4">Programs ({selectedLender.programs.length})</h3>
              <div className="space-y-4">
                {selectedLender.programs.map((program, idx) => (
                  <div key={idx}>
                    <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
                      <h4 className="text-lg font-bold text-white mb-4">{program.name}</h4>

                      <table className="w-full text-sm">
                        <thead>
                          <tr className="border-b border-slate-700">
                            <th className="text-left p-3">Criteria</th>
                            <th className="text-left p-3">Value</th>
                          </tr>
                        </thead>
                        <tbody>
                          <tr>
                            <td className="p-3 text-slate-400">Min Amount</td>
                            <td className="p-3 text-white font-semibold">{program.minAmount ? `$${program.minAmount.toLocaleString()}` : 'None'}</td>
                          </tr>
                          <tr>
                            <td className="p-3 text-slate-400">Max Amount</td>
                            <td className="p-3 text-white font-semibold">{program.maxAmount ? `$${program.maxAmount.toLocaleString()}` : 'None'}</td>
                          </tr>
                          <tr>
                            <td className="p-3 text-slate-400">Min FICO</td>
                            <td className="p-3 text-white font-semibold">{program.minFico || 'None'}</td>
                          </tr>
                          <tr>
                            <td className="p-3 text-slate-400">Min PayNet</td>
                            <td className="p-3 text-white font-semibold">{program.minPayNet || 'None'}</td>
                          </tr>
                          <tr>
                            <td className="p-3 text-slate-400">Min Time In Business (years)</td>
                            <td className="p-3 text-white font-semibold">{program.minTimeInBusinessYears || 'None'}</td>
                          </tr>
                          <tr>
                            <td className="p-3 text-slate-400">Min Revenue</td>
                            <td className="p-3 text-white font-semibold">{program.minRevenue ? `$${program.minRevenue.toLocaleString()}` : 'None'}</td>
                          </tr>
                          <tr>
                            <td className="p-3 text-slate-400">Max Equipment Age</td>
                            <td className="p-3 text-white font-semibold">{program.maxEquipmentAgeYears ? `${program.maxEquipmentAgeYears} years` : 'None'}</td>
                          </tr>
                          <tr>
                            <td className="p-3 text-slate-400">Excludes Trucking</td>
                            <td className="p-3 text-white font-semibold">{program.excludeTrucking ? 'Yes' : 'No'}</td>
                          </tr>
                        </tbody>
                      </table>
                    </div>
                    {idx < selectedLender.programs.length - 1 && (
                      <div className="border-t-2 border-dashed border-slate-600 my-6" />
                    )}
                  </div>
                ))}
              </div>
            </div>
          </div>
        ) : (
          <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-12 border border-slate-700/50 text-center">
            <div className="text-slate-500 text-lg">Select a lender to view policies</div>
          </div>
        )}
      </div>
    </div>
  );
}